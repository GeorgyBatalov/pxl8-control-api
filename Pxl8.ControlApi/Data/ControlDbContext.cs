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

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Domain> Domains => Set<Domain>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<BillingPeriod> BillingPeriods => Set<BillingPeriod>();
    public DbSet<BudgetLease> BudgetLeases => Set<BudgetLease>();
    public DbSet<UsageReport> UsageReports => Set<UsageReport>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tenant
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.SuspensionReason).HasColumnName("suspension_reason").HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            // Indexes
            entity.HasIndex(e => e.Email).HasDatabaseName("ix_tenants_email");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("ix_tenants_is_active");
        });

        // Domain
        modelBuilder.Entity<Domain>(entity =>
        {
            entity.ToTable("domains");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.DomainName).HasColumnName("domain_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.IsVerified).HasColumnName("is_verified");
            entity.Property(e => e.VerificationMethod).HasColumnName("verification_method").HasMaxLength(10);
            entity.Property(e => e.VerificationToken).HasColumnName("verification_token").HasMaxLength(100);
            entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            // Indexes
            entity.HasIndex(e => e.DomainName).IsUnique().HasDatabaseName("ix_domains_domain_name");
            entity.HasIndex(e => e.TenantId).HasDatabaseName("ix_domains_tenant_id");
        });

        // ApiKey
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.ToTable("api_keys");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.KeyHash).HasColumnName("key_hash").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.LastUsedAt).HasColumnName("last_used_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");

            // Indexes
            entity.HasIndex(e => e.TenantId).HasDatabaseName("ix_api_keys_tenant_id");
            entity.HasIndex(e => new { e.TenantId, e.IsActive }).HasDatabaseName("ix_api_keys_tenant_active");
        });

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
