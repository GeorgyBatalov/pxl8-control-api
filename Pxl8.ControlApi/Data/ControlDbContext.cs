using Microsoft.EntityFrameworkCore;
using Pxl8.ControlApi.Data.Entities;

namespace Pxl8.ControlApi.Data;

/// <summary>
/// Control Plane database context
/// </summary>
public class ControlDbContext : DbContext
{
    public ControlDbContext(DbContextOptions<ControlDbContext> options) : base(options)
    {
    }

    public DbSet<BillingPeriod> BillingPeriods => Set<BillingPeriod>();
    public DbSet<BudgetLease> BudgetLeases => Set<BudgetLease>();
    public DbSet<UsageReport> UsageReports => Set<UsageReport>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // BillingPeriod
        modelBuilder.Entity<BillingPeriod>(entity =>
        {
            entity.ToTable("billing_periods");
            entity.HasKey(e => e.PeriodId);

            entity.Property(e => e.PeriodId).HasColumnName("period_id");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.PeriodKey).HasColumnName("period_key").HasMaxLength(20).IsRequired();
            entity.Property(e => e.StartsAt).HasColumnName("starts_at");
            entity.Property(e => e.EndsAt).HasColumnName("ends_at");
            entity.Property(e => e.BandwidthConsumedBytes).HasColumnName("bandwidth_consumed_bytes");
            entity.Property(e => e.TransformsConsumed).HasColumnName("transforms_consumed");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            // Indexes
            entity.HasIndex(e => new { e.TenantId, e.PeriodKey }).HasDatabaseName("ix_billing_periods_tenant_period_key");
        });

        // BudgetLease
        modelBuilder.Entity<BudgetLease>(entity =>
        {
            entity.ToTable("budget_leases");
            entity.HasKey(e => e.LeaseId);

            entity.Property(e => e.LeaseId).HasColumnName("lease_id");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.PeriodId).HasColumnName("period_id");
            entity.Property(e => e.DataplaneId).HasColumnName("dataplane_id").HasMaxLength(100).IsRequired();
            entity.Property(e => e.BandwidthGrantedBytes).HasColumnName("bandwidth_granted_bytes");
            entity.Property(e => e.TransformsGranted).HasColumnName("transforms_granted");
            entity.Property(e => e.GrantedAt).HasColumnName("granted_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.RequestId).HasColumnName("request_id");

            // Indexes
            entity.HasIndex(e => e.RequestId).IsUnique().HasDatabaseName("ix_budget_leases_request_id");
            entity.HasIndex(e => new { e.TenantId, e.PeriodId, e.DataplaneId, e.Status })
                .HasDatabaseName("ix_budget_leases_tenant_period_dataplane_status");

            // CRITICAL: Only ONE active lease per (tenant, period, dataplane)
            entity.HasIndex(e => new { e.TenantId, e.PeriodId, e.DataplaneId })
                .IsUnique()
                .HasDatabaseName("ix_budget_leases_active_unique")
                .HasFilter("status = 'Active'");
        });

        // UsageReport
        modelBuilder.Entity<UsageReport>(entity =>
        {
            entity.ToTable("usage_reports");
            entity.HasKey(e => e.ReportId);

            entity.Property(e => e.ReportId).HasColumnName("report_id");
            entity.Property(e => e.DataplaneId).HasColumnName("dataplane_id").HasMaxLength(100).IsRequired();
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.PeriodId).HasColumnName("period_id");
            entity.Property(e => e.BandwidthUsedBytes).HasColumnName("bandwidth_used_bytes");
            entity.Property(e => e.TransformsUsed).HasColumnName("transforms_used");
            entity.Property(e => e.ReportedAt).HasColumnName("reported_at");
            entity.Property(e => e.ReceivedAt).HasColumnName("received_at");

            // Indexes
            entity.HasIndex(e => e.ReportId).IsUnique().HasDatabaseName("ix_usage_reports_report_id");
            entity.HasIndex(e => new { e.TenantId, e.PeriodId, e.ReceivedAt })
                .HasDatabaseName("ix_usage_reports_tenant_period_received");
        });

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            // Indexes
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("ix_users_email");
            entity.HasIndex(e => e.TenantId).HasDatabaseName("ix_users_tenant_id");
        });
    }
}
