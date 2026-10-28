using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Xunit;

namespace LimousineBooking.Tests.Domain;

public class CrmActivityTests
{
    [Fact]
    public void Constructor_WithNoAssociation_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new CrmActivity(CrmActivityType.Call, "Checked in", DateTime.UtcNow, Guid.NewGuid(),
                companyId: null, contactId: null, dealId: null, description: null, outcome: null));
    }

    [Fact]
    public void Constructor_WithACompanyAssociation_Succeeds()
    {
        var activity = new CrmActivity(CrmActivityType.Call, "Checked in", DateTime.UtcNow, Guid.NewGuid(),
            companyId: Guid.NewGuid(), contactId: null, dealId: null, description: null, outcome: null);

        Assert.NotNull(activity.CompanyId);
    }

    [Fact]
    public void Constructor_WithEmptySubject_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new CrmActivity(CrmActivityType.Call, "  ", DateTime.UtcNow, Guid.NewGuid(),
                companyId: Guid.NewGuid(), contactId: null, dealId: null, description: null, outcome: null));
    }
}
