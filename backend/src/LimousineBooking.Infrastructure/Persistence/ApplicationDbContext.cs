using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Domain.Entities.Route> Routes => Set<Domain.Entities.Route>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingStatusHistory> BookingStatusHistories => Set<BookingStatusHistory>();
    public DbSet<DriverAvailability> DriverAvailabilities => Set<DriverAvailability>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AssignmentHistory> AssignmentHistories => Set<AssignmentHistory>();
    public DbSet<RideStatusHistory> RideStatusHistories => Set<RideStatusHistory>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentWebhookEvent> PaymentWebhookEvents => Set<PaymentWebhookEvent>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<CrmCompany> CrmCompanies => Set<CrmCompany>();
    public DbSet<CrmContact> CrmContacts => Set<CrmContact>();
    public DbSet<CrmCompanyContact> CrmCompanyContacts => Set<CrmCompanyContact>();
    public DbSet<CrmPipeline> CrmPipelines => Set<CrmPipeline>();
    public DbSet<CrmPipelineStage> CrmPipelineStages => Set<CrmPipelineStage>();
    public DbSet<CrmDeal> CrmDeals => Set<CrmDeal>();
    public DbSet<CrmDealContact> CrmDealContacts => Set<CrmDealContact>();
    public DbSet<CrmActivity> CrmActivities => Set<CrmActivity>();
    public DbSet<CrmTask> CrmTasks => Set<CrmTask>();
    public DbSet<EmailProviderConnection> EmailProviderConnections => Set<EmailProviderConnection>();
    public DbSet<EmailSyncCheckpoint> EmailSyncCheckpoints => Set<EmailSyncCheckpoint>();
    public DbSet<CrmEmailThread> CrmEmailThreads => Set<CrmEmailThread>();
    public DbSet<CrmEmailThreadCompany> CrmEmailThreadCompanies => Set<CrmEmailThreadCompany>();
    public DbSet<CrmEmailThreadContact> CrmEmailThreadContacts => Set<CrmEmailThreadContact>();
    public DbSet<CrmEmailThreadDeal> CrmEmailThreadDeals => Set<CrmEmailThreadDeal>();
    public DbSet<CrmEmailMessage> CrmEmailMessages => Set<CrmEmailMessage>();
    public DbSet<CrmEmailAttachment> CrmEmailAttachments => Set<CrmEmailAttachment>();
    public DbSet<EmailSendOperation> EmailSendOperations => Set<EmailSendOperation>();
    public DbSet<CrmEmailTemplate> CrmEmailTemplates => Set<CrmEmailTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
