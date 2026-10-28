using System.Net;
using System.Web;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using LimousineBooking.Application.Email;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LimousineBooking.Infrastructure.Email;

/// <summary>
/// Stage 2A's only real IEmailProviderClient implementation, using Google's
/// official Google.Apis.Gmail.v1 / Google.Apis.Auth SDKs — the same
/// "use the official provider SDK, don't hand-roll its HTTP API" convention
/// this app already follows for Stripe. Scopes requested are the minimum
/// needed: gmail.modify (read + mark read/unread) and gmail.send.
/// </summary>
public class GmailProviderClient : IEmailProviderClient
{
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string RevokeEndpoint = "https://oauth2.googleapis.com/revoke";
    private static readonly string[] Scopes = { GmailService.Scope.GmailModify, GmailService.Scope.GmailSend };

    private readonly GmailOAuthSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GmailProviderClient> _logger;

    public GmailProviderClient(IOptions<GmailOAuthSettings> settings, IHttpClientFactory httpClientFactory, ILogger<GmailProviderClient> logger)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public EmailProvider Provider => EmailProvider.Gmail;

    public string BuildAuthorizationUrl(string state, string redirectUri)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = _settings.ClientId;
        query["redirect_uri"] = redirectUri;
        query["response_type"] = "code";
        query["scope"] = string.Join(' ', Scopes);
        query["state"] = state;
        query["access_type"] = "offline";
        // Forces Google to re-issue a refresh token even if this mailbox has
        // already granted consent before — without this, a reconnect after a
        // revoke could silently come back with no refresh token at all.
        query["prompt"] = "consent";

        return $"{AuthorizationEndpoint}?{query}";
    }

    public async Task<EmailProviderTokenResult> ExchangeCodeForTokensAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        using var flow = CreateFlow();
        TokenResponse tokenResponse;
        try
        {
            tokenResponse = await flow.ExchangeCodeForTokenAsync("gmail-connection", code, redirectUri, cancellationToken);
        }
        catch (TokenResponseException ex)
        {
            throw new InvalidOperationException($"Gmail rejected the authorization code: {ex.Error?.ErrorDescription ?? ex.Message}", ex);
        }

        var emailAddress = await GetConnectedEmailAddressAsync(tokenResponse.AccessToken, cancellationToken);

        return ToResult(tokenResponse, emailAddress);
    }

    public async Task<EmailProviderTokenResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        using var flow = CreateFlow();
        TokenResponse tokenResponse;
        try
        {
            tokenResponse = await flow.RefreshTokenAsync("gmail-connection", refreshToken, cancellationToken);
        }
        catch (TokenResponseException ex)
        {
            throw new EmailReauthRequiredException($"Gmail rejected the refresh token: {ex.Error?.ErrorDescription ?? ex.Message}", ex);
        }

        return ToResult(tokenResponse, emailAddress: null);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = refreshToken });
            await client.PostAsync(RevokeEndpoint, content, cancellationToken);
        }
        catch (Exception ex)
        {
            // Best-effort — the caller always clears local tokens regardless of whether the provider-side revoke succeeded.
            _logger.LogWarning(ex, "Failed to revoke the Gmail refresh token at the provider — local tokens will still be cleared.");
        }
    }

    public async Task<ProviderSyncPage> FetchIncrementalChangesAsync(string accessToken, string? cursor, CancellationToken cancellationToken = default)
    {
        var service = CreateService(accessToken);

        if (cursor is null)
            return await FullResyncAsync(service, cancellationToken);

        if (!ulong.TryParse(cursor, out var startHistoryId))
            return await FullResyncAsync(service, cancellationToken);

        var changed = new Dictionary<string, string>();
        string? pageToken = null;
        ulong? latestHistoryId = null;

        do
        {
            var request = service.Users.History.List("me");
            request.StartHistoryId = startHistoryId;
            request.PageToken = pageToken;

            ListHistoryResponse response;
            try
            {
                response = await request.ExecuteAsync(cancellationToken);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            {
                // Gmail expires history after ~7 days — the stored cursor is no longer usable.
                return new ProviderSyncPage { CursorInvalid = true };
            }

            latestHistoryId = response.HistoryId ?? latestHistoryId;

            if (response.History is not null)
            {
                foreach (var entry in response.History)
                {
                    foreach (var added in entry.MessagesAdded ?? new List<HistoryMessageAdded>())
                        changed[added.Message.Id] = added.Message.ThreadId;
                }
            }

            pageToken = response.NextPageToken;
        } while (pageToken is not null);

        return new ProviderSyncPage
        {
            ChangedMessages = changed.Select(kvp => new ProviderMessageSummary { ProviderMessageId = kvp.Key, ProviderThreadId = kvp.Value }).ToList(),
            NewCursor = latestHistoryId?.ToString()
        };
    }

    public async Task<ProviderMessageDetail> GetMessageAsync(string accessToken, string providerMessageId, CancellationToken cancellationToken = default)
    {
        var service = CreateService(accessToken);
        var request = service.Users.Messages.Get("me", providerMessageId);
        request.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;

        var message = await request.ExecuteAsync(cancellationToken);
        return GmailMessageParser.Parse(message);
    }

    public async Task<ProviderSendResult> SendMessageAsync(string accessToken, OutgoingEmailMessage message, CancellationToken cancellationToken = default)
    {
        var service = CreateService(accessToken);
        var raw = GmailMimeMessageBuilder.BuildRawBase64Url(message);

        try
        {
            var sent = await service.Users.Messages.Send(new Message { Raw = raw, ThreadId = message.ProviderThreadId }, "me").ExecuteAsync(cancellationToken);
            return new ProviderSendResult { Succeeded = true, ProviderMessageId = sent.Id };
        }
        catch (GoogleApiException ex) when (IsAmbiguousFailure(ex))
        {
            _logger.LogWarning(ex, "Gmail send returned an ambiguous result (status {Status}) — caller must reconcile via Message-ID before retrying.", ex.HttpStatusCode);
            return new ProviderSendResult { Succeeded = false, IsAmbiguous = true, ErrorMessage = ex.Message };
        }
        catch (GoogleApiException ex)
        {
            return new ProviderSendResult { Succeeded = false, ErrorMessage = ex.Message };
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // A client-side timeout (not our own cancellation) is exactly the "we don't know if it arrived" case.
            return new ProviderSendResult { Succeeded = false, IsAmbiguous = true, ErrorMessage = "The request to Gmail timed out before a response was received." };
        }
    }

    public async Task<ProviderMessageDetail?> FindMessageByMessageIdHeaderAsync(string accessToken, string messageIdHeader, CancellationToken cancellationToken = default)
    {
        var service = CreateService(accessToken);
        var request = service.Users.Messages.List("me");
        request.Q = $"rfc822msgid:{messageIdHeader}";

        var list = await request.ExecuteAsync(cancellationToken);
        var match = list.Messages?.FirstOrDefault();
        if (match is null)
            return null;

        return await GetMessageAsync(accessToken, match.Id, cancellationToken);
    }

    public async Task<byte[]> GetAttachmentAsync(string accessToken, string providerMessageId, string providerAttachmentId, CancellationToken cancellationToken = default)
    {
        var service = CreateService(accessToken);
        var attachment = await service.Users.Messages.Attachments.Get("me", providerMessageId, providerAttachmentId).ExecuteAsync(cancellationToken);

        var normalized = attachment.Data.Replace('-', '+').Replace('_', '/');
        var padding = normalized.Length % 4;
        if (padding > 0)
            normalized += new string('=', 4 - padding);

        return Convert.FromBase64String(normalized);
    }

    private async Task<ProviderSyncPage> FullResyncAsync(GmailService service, CancellationToken cancellationToken)
    {
        var changed = new Dictionary<string, string>();
        string? pageToken = null;

        do
        {
            var request = service.Users.Messages.List("me");
            request.LabelIds = new List<string> { "INBOX", "SENT" };
            request.PageToken = pageToken;

            var response = await request.ExecuteAsync(cancellationToken);
            foreach (var summary in response.Messages ?? new List<Message>())
                changed[summary.Id] = summary.ThreadId;

            pageToken = response.NextPageToken;
        } while (pageToken is not null);

        var profile = await service.Users.GetProfile("me").ExecuteAsync(cancellationToken);

        return new ProviderSyncPage
        {
            ChangedMessages = changed.Select(kvp => new ProviderMessageSummary { ProviderMessageId = kvp.Key, ProviderThreadId = kvp.Value }).ToList(),
            NewCursor = profile.HistoryId?.ToString()
        };
    }

    private async Task<string?> GetConnectedEmailAddressAsync(string accessToken, CancellationToken cancellationToken)
    {
        var service = CreateService(accessToken);
        var profile = await service.Users.GetProfile("me").ExecuteAsync(cancellationToken);
        return profile.EmailAddress;
    }

    private GmailService CreateService(string accessToken) => new(new BaseClientService.Initializer
    {
        ApplicationName = "ROI Limousinen CRM",
        HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken)
    });

    private GoogleAuthorizationCodeFlow CreateFlow() => new(new GoogleAuthorizationCodeFlow.Initializer
    {
        ClientSecrets = new ClientSecrets { ClientId = _settings.ClientId, ClientSecret = _settings.ClientSecret },
        Scopes = Scopes,
        DataStore = new NullDataStore()
    });

    private static EmailProviderTokenResult ToResult(TokenResponse tokenResponse, string? emailAddress) => new()
    {
        AccessToken = tokenResponse.AccessToken,
        RefreshToken = tokenResponse.RefreshToken,
        AccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds ?? 3600),
        EmailAddress = emailAddress
    };

    private static bool IsAmbiguousFailure(GoogleApiException ex) =>
        ex.HttpStatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout or HttpStatusCode.RequestTimeout;
}
