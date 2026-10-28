using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Xunit;

namespace LimousineBooking.Tests.Domain;

public class CrmTaskTests
{
    private static CrmTask CreateTask() =>
        new("Call the hotel GM", new DateOnly(2026, 9, 10), Guid.NewGuid(), CrmPriority.High, null, null, null, null, null);

    [Fact]
    public void Complete_SetsStatusAndCompletedAt()
    {
        var task = CreateTask();

        task.Complete();

        Assert.Equal(CrmTaskStatus.Completed, task.Status);
        Assert.NotNull(task.CompletedAt);
    }

    [Fact]
    public void Cancel_SetsStatus_AndClearsCompletedAt()
    {
        var task = CreateTask();
        task.Complete();

        task.Cancel();

        Assert.Equal(CrmTaskStatus.Cancelled, task.Status);
        Assert.Null(task.CompletedAt);
    }

    [Fact]
    public void Constructor_WithEmptyTitle_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new CrmTask("  ", new DateOnly(2026, 9, 10), Guid.NewGuid(), CrmPriority.Normal, null, null, null, null, null));
    }

    [Fact]
    public void Constructor_WithEmptyAssignedUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new CrmTask("Follow up", new DateOnly(2026, 9, 10), Guid.Empty, CrmPriority.Normal, null, null, null, null, null));
    }
}
