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
        // 1. Get all active tenant IDs from BillingPeriods
        var activeTenantIds = await _db.BillingPeriods
            .Select(p => p.TenantId)
            .Distinct()
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Generating policy snapshot for {TenantCount} active tenants", activeTenantIds.Count);

        // 2. Generate TenantPolicyDto for each tenant
        var tenantPolicies = activeTenantIds.Select(tenantId => new TenantPolicyDto
        {
            TenantId = tenantId,
            Status = "active", // TODO: Load from Tenants table
            PlanCode = "free", // TODO: Load from Tenants table
            Quotas = new QuotasDto
            {
                BandwidthLimitBytes = DefaultBandwidthLimitBytes,
                TransformsLimit = DefaultTransformsLimit,
                StorageLimitBytes = DefaultStorageLimitBytes,
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
