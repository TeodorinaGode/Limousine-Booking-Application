using LimousineBooking.Domain.Entities;
using Xunit;

namespace LimousineBooking.Tests.Domain;

public class CrmDealTests
{
    private static CrmDeal CreateDeal() =>
        new("Annual chauffeur contract", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 50000m, "CHF");

    [Fact]
    public void MoveToStage_IntoLostStage_WithoutReason_Throws()
    {
        var deal = CreateDeal();

        Assert.Throws<ArgumentException>(() => deal.MoveToStage(Guid.NewGuid(), 0, isWon: false, isLost: true, lossReason: null));
    }

    [Fact]
    public void MoveToStage_IntoLostStage_WithReason_SetsClosedAtAndLossReason()
    {
        var deal = CreateDeal();

        deal.MoveToStage(Guid.NewGuid(), 0, isWon: false, isLost: true, lossReason: "Budget cut");

        Assert.NotNull(deal.ClosedAt);
        Assert.Equal("Budget cut", deal.LossReason);
    }

    [Fact]
    public void MoveToStage_IntoWonStage_SetsClosedAt_AndClearsLossReason()
    {
        var deal = CreateDeal();
        deal.MoveToStage(Guid.NewGuid(), 0, isWon: false, isLost: true, lossReason: "Went quiet");

        deal.MoveToStage(Guid.NewGuid(), 100, isWon: true, isLost: false, lossReason: null);

        Assert.NotNull(deal.ClosedAt);
        Assert.Null(deal.LossReason);
    }

    [Fact]
    public void MoveToStage_ReopeningFromAClosedStage_ClearsClosedAtAndLossReason()
    {
        var deal = CreateDeal();
        deal.MoveToStage(Guid.NewGuid(), 0, isWon: false, isLost: true, lossReason: "Went quiet");

        deal.MoveToStage(Guid.NewGuid(), 35, isWon: false, isLost: false, lossReason: null);

        Assert.Null(deal.ClosedAt);
        Assert.Null(deal.LossReason);
    }

    [Fact]
    public void MoveToStage_SetsProbabilityToTheGivenValue()
    {
        var deal = CreateDeal();

        deal.MoveToStage(Guid.NewGuid(), 60, isWon: false, isLost: false, lossReason: null);

        Assert.Equal(60, deal.Probability);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void MoveToStage_WithOutOfRangeProbability_Throws(int probability)
    {
        var deal = CreateDeal();

        Assert.Throws<ArgumentException>(() => deal.MoveToStage(Guid.NewGuid(), probability, isWon: false, isLost: false, lossReason: null));
    }

    [Fact]
    public void Constructor_WithNegativeValue_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new CrmDeal("Deal", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -1m, "CHF"));
    }
}
