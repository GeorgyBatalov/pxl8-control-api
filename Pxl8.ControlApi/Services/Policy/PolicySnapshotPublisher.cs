using Microsoft.EntityFrameworkCore;
using Pxl8.ControlApi.Contracts.V1.PolicySnapshot;
using Pxl8.ControlApi.Data;

namespace Pxl8.ControlApi.Services.Policy;

/// <summary>
/// Policy snapshot publisher service - generate atomic policy snapshots
/// </summary>
/// <remarks>
/// MVP implementation: generates policies from BillingPeriods (active tenants)
/// TODO: Add proper Tenants table with quota configuration
/// </remarks>
public class PolicySnapshotPublisher : IPolicySnapshotPublisher
{
    private readonly ControlDbContext _db;
    private readonly ILogger<PolicySnapshotPublisher> _logger;

    // TODO: Get from tenant configuration (Tenants table)
    // For now, hardcoded default limits
    private const long DefaultBandwidthLimitBytes = 107_374_182_400; // 100 GB
    private const int DefaultTransformsLimit = 1_000_000;
    private const long DefaultStorageLimitBytes = 53_687_091_200; // 50 GB

    public PolicySnapshotPublisher(ControlDbContext db, ILogger<PolicySnapshotPublisher> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PolicySnapshotDto> GenerateSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        // 1. Get all active tenants with their current billing period
        var tenantsWithPeriods = await _db.BillingPeriods
            .GroupBy(p => p.TenantId)
            .Select(g => new
            {
                TenantId = g.Key,
                // Current period: active now OR most recent
                CurrentPeriod = g
                    .Where(p => p.StartsAt <= now && now < p.EndsAt)
                    .OrderByDescending(p => p.StartsAt)
                    .FirstOrDefault() ?? g.OrderByDescending(p => p.StartsAt).First()
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Generating policy snapshot for {TenantCount} active tenants", tenantsWithPeriods.Count);

        // 2. Generate TenantPolicyDto for each tenant
        var tenantPolicies = tenantsWithPeriods.Select(t => new TenantPolicyDto
        {
            TenantId = t.TenantId,
            CurrentPeriodId = t.CurrentPeriod.PeriodId,
            Status = "active", // TODO: Load from Tenants table
            PlanCode = "free", // TODO: Load from Tenants table
            Quotas = new QuotasDto
            {
                BandwidthLimitBytes = t.CurrentPeriod.BandwidthLimit,
                TransformsLimit = t.CurrentPeriod.TransformsLimit,
                StorageLimitBytes = t.CurrentPeriod.StorageLimit,
                DomainsLimit = 10
            },
            Domains = new List<DomainDto>(), // TODO: Load from Domains table
            ApiKeys = new List<ApiKeyDto>() // TODO: Load from ApiKeys table
        }).ToList();

        // 3. Create snapshot
        var snapshot = new PolicySnapshotDto
        {
            SnapshotId = Guid.NewGuid(),
            GeneratedAt = DateTimeOffset.UtcNow,
            Tenants = tenantPolicies
        };

        _logger.LogInformation(
            "Policy snapshot generated: snapshot_id={SnapshotId}, tenant_count={TenantCount}",
            snapshot.SnapshotId, tenantPolicies.Count);

        return snapshot;
    }
}
