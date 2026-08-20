using System.Reflection;
using System.Text;
using LimousineBooking.Api.Json;
using LimousineBooking.Application;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Infrastructure;
using LimousineBooking.Infrastructure.Authentication;
using LimousineBooking.Infrastructure.BackgroundServices;
using LimousineBooking.Infrastructure.Persistence;
using LimousineBooking.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new FlexibleTimeOnlyJsonConverter()));

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// The "Testing" environment (WebApplicationFactory-based integration tests)
// never needs a live background poller — it would just add DB traffic and
// log noise against a database the tests don't otherwise touch.
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<NotificationOutboxWorker>();
    builder.Services.AddHostedService<ContactMessageOutboxWorker>();
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// JwtBearerOptions is configured via a deferred callback (resolving
// IOptions<JwtSettings> at the time options are actually built) rather than
// closing over a value read from IConfiguration up front. This keeps the
// token-validating side and the token-issuing side (JwtTokenService, which
// also resolves IOptions<JwtSettings>) guaranteed to agree — including when
// configuration is layered in after this point, as WebApplicationFactory
// does in integration tests.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((options, jwtSettingsOptions) =>
    {
        var jwtSettings = jwtSettingsOptions.Value;

        // Tokens are issued with short claim names ("sub", "email", "role", "name" —
        // see JwtTokenService) rather than the long ClaimTypes.* URIs. Disabling the
        // default inbound claim-type remapping keeps what's read consistent with what
        // was written, and RoleClaimType must match "role" for [Authorize(Roles=...)]
        // to recognize it.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                string.IsNullOrEmpty(jwtSettings.SecretKey) ? Guid.NewGuid().ToString() : jwtSettings.SecretKey)),
            RoleClaimType = "role",
            NameClaimType = "email"
        };
    });

builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Limousine Booking API", Version = "v1" });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Enter a valid JWT token",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });

    // Request/response DTOs live in LimousineBooking.Application, not this
    // assembly, so both projects' XML doc files are needed for Swagger to
    // pick up property-level descriptions (validation rules, etc.).
    foreach (var assemblyName in new[] { Assembly.GetExecutingAssembly().GetName().Name, "LimousineBooking.Application" })
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.xml");
        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    try
    {
        using var seedScope = app.Services.CreateScope();
        var dbContext = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordService = seedScope.ServiceProvider.GetRequiredService<IPasswordService>();
        await DevelopmentDataSeeder.SeedAsync(dbContext, passwordService);
    }
    catch (Exception ex)
    {
        // Dev-only convenience seeding must not prevent the API from starting
        // when PostgreSQL isn't reachable yet (e.g. running the API without
        // `docker compose up postgres` first).
        app.Logger.LogWarning(ex, "Skipped development user seeding — database was not reachable.");
    }
}
else
{
    // Outside Development, ASP.NET Core has no automatic exception page —
    // without this, unhandled exceptions still return 500 but with no body.
    // This gives a consistent JSON error shape without leaking stack traces.
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = "An unexpected error occurred." });
        });
    });
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
