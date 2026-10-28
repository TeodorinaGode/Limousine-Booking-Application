using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Xunit;

namespace LimousineBooking.Tests.Domain;

public class EmailSyncCheckpointTests
{
    [Fact]
    public void IsClaimable_WhenNotRunning_IsTrue()
    {
        var checkpoint = new EmailSyncCheckpoint(Guid.NewGuid());

        Assert.True(checkpoint.IsClaimable(DateTime.UtcNow, staleRunningMinutes: 10));
    }

    [Fact]
    public void IsClaimable_WhenRunningRecently_IsFalse()
    {
        var checkpoint = new EmailSyncCheckpoint(Guid.NewGuid());
        var now = DateTime.UtcNow;
        checkpoint.BeginRun(now);

        Assert.False(checkpoint.IsClaimable(now.AddMinutes(2), staleRunningMinutes: 10));
    }

    [Fact]
    public void IsClaimable_WhenRunStartedLongAgo_IsReclaimable()
    {
        var checkpoint = new EmailSyncCheckpoint(Guid.NewGuid());
        var startedAt = DateTime.UtcNow;
        checkpoint.BeginRun(startedAt);

        Assert.True(checkpoint.IsClaimable(startedAt.AddMinutes(15), staleRunningMinutes: 10));
    }

    [Fact]
    public void CompleteRun_WithNoNewCursor_KeepsExistingCursor()
    {
        var checkpoint = new EmailSyncCheckpoint(Guid.NewGuid());
        checkpoint.BeginRun(DateTime.UtcNow);
        checkpoint.CompleteRun(DateTime.UtcNow, "cursor-1", 5);

        checkpoint.BeginRun(DateTime.UtcNow);
        checkpoint.CompleteRun(DateTime.UtcNow, newCursor: null, messagesSynced: 0);

        Assert.Equal("cursor-1", checkpoint.ProviderCursor);
        Assert.False(checkpoint.IsRunning);
        Assert.Equal(EmailSyncRunStatus.Success, checkpoint.LastRunStatus);
    }

    [Fact]
    public void FailRun_SetsFailedStatusAndReleasesLock()
    {
        var checkpoint = new EmailSyncCheckpoint(Guid.NewGuid());
        checkpoint.BeginRun(DateTime.UtcNow);

        checkpoint.FailRun(DateTime.UtcNow, "Gmail returned 500.");

        Assert.False(checkpoint.IsRunning);
        Assert.Equal(EmailSyncRunStatus.Failed, checkpoint.LastRunStatus);
        Assert.Equal("Gmail returned 500.", checkpoint.LastError);
    }

    [Fact]
    public void InvalidateCursor_ClearsProviderCursor()
    {
        var checkpoint = new EmailSyncCheckpoint(Guid.NewGuid());
        checkpoint.BeginRun(DateTime.UtcNow);
        checkpoint.CompleteRun(DateTime.UtcNow, "cursor-1", 1);

        checkpoint.InvalidateCursor();

        Assert.Null(checkpoint.ProviderCursor);
    }
}
