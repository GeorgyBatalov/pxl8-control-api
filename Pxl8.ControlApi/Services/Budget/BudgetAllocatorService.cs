using Microsoft.EntityFrameworkCore;
using Pxl8.ControlApi.Contracts.V1.BudgetAllocation;
using Pxl8.ControlApi.Data;
using Pxl8.ControlApi.Data.Entities;

namespace Pxl8.ControlApi.Services.Budget;

/// <summary>
/// Budget allocator service - allocate TTL-based budget leases
/// </summary>
public class BudgetAllocatorService : IBudgetAllocatorService
{
    private readonly ControlDbContext _db;
    private readonly ILogger<BudgetAllocatorService> _logger;

    // Lease TTL: 5 minutes
    private static readonly TimeSpan LeaseTtl = TimeSpan.FromMinutes(5);

    // TODO: Get from tenant/plan configuration
    // For now, hardcoded limits (100 GB bandwidth, 1M transforms per period)
    private const long DefaultBandwidthLimit = 107_374_182_400; // 100 GB
    private const int DefaultTransformsLimit = 1_000_000;

    public BudgetAllocatorService(ControlDbContext db, ILogger<BudgetAllocatorService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<BudgetAllocateResponse> AllocateBudgetAsync(
        BudgetAllocateRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Check idempotency - if request_id exists, return existing lease
        var existingLease = await _db.BudgetLeases
            .FirstOrDefaultAsync(l => l.RequestId == request.RequestId, cancellationToken);

        if (existingLease != null)
        {
            _logger.LogInformation(
                "Idempotent request: returning existing lease {LeaseId} for request_id {RequestId}",
                existingLease.LeaseId, request.RequestId);

            return MapToResponse(existingLease);
        }

        // 2. Get or create billing period
        var period = await GetOrCreateBillingPeriodAsync(request.TenantId, request.PeriodId, cancellationToken);

        // 3. Calculate consumed budget
        var consumedBandwidth = period.BandwidthConsumedBytes;
        var consumedTransforms = period.TransformsConsumed;

        // 4. Calculate active leases (sum of all active leases for this tenant/period)
        var activeLeasesSum = await _db.BudgetLeases
            .Where(l => l.TenantId == request.TenantId
                        && l.PeriodId == request.PeriodId
                        && l.Status == "Active"
                        && l.ExpiresAt > DateTimeOffset.UtcNow)
            .GroupBy(l => 1)
            .Select(g => new
            {
                BandwidthLeased = g.Sum(l => l.BandwidthGrantedBytes),
                TransformsLeased = g.Sum(l => l.TransformsGranted)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var leasedBandwidth = activeLeasesSum?.BandwidthLeased ?? 0;
        var leasedTransforms = activeLeasesSum?.TransformsLeased ?? 0;

        // 5. Calculate available budget
        // available = limit - consumed - leased_active
        var availableBandwidth = Math.Max(0, DefaultBandwidthLimit - consumedBandwidth - leasedBandwidth);
        var availableTransforms = Math.Max(0, DefaultTransformsLimit - consumedTransforms - leasedTransforms);

        // 6. Grant budget: min(requested, available)
        var grantedBandwidth = Math.Min(request.BandwidthRequestedBytes, availableBandwidth);
        var grantedTransforms = Math.Min(request.TransformsRequested, availableTransforms);

        _logger.LogInformation(
            "Budget allocation for tenant {TenantId}: requested={RequestedBw}bytes/{RequestedTx}tx, " +
            "available={AvailableBw}/{AvailableTx}, granted={GrantedBw}/{GrantedTx}",
            request.TenantId, request.BandwidthRequestedBytes, request.TransformsRequested,
            availableBandwidth, availableTransforms, grantedBandwidth, grantedTransforms);

        // 7. Revoke old active lease for same (tenant, period, dataplane) if exists
        var oldLease = await _db.BudgetLeases
            .FirstOrDefaultAsync(l =>
                l.TenantId == request.TenantId
                && l.PeriodId == request.PeriodId
                && l.DataplaneId == request.DataplaneId
                && l.Status == "Active",
                cancellationToken);

        if (oldLease != null)
        {
            oldLease.Status = "Revoked";
            oldLease.RevokedAt = DateTimeOffset.UtcNow;

            _logger.LogInformation(
                "Revoked old lease {LeaseId} for new allocation (tenant={TenantId}, dataplane={DataplaneId})",
                oldLease.LeaseId, request.TenantId, request.DataplaneId);
        }

        // 8. Create new lease
        var now = DateTimeOffset.UtcNow;
        var newLease = new BudgetLease
        {
            LeaseId = Guid.NewGuid(),
            TenantId = request.TenantId,
            PeriodId = request.PeriodId,
            DataplaneId = request.DataplaneId,
            BandwidthGrantedBytes = grantedBandwidth,
            TransformsGranted = grantedTransforms,
            GrantedAt = now,
            ExpiresAt = now.Add(LeaseTtl),
            Status = "Active",
            RequestId = request.RequestId
        };

        _db.BudgetLeases.Add(newLease);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Budget lease allocated: lease_id={LeaseId}, tenant={TenantId}, dataplane={DataplaneId}, " +
            "bandwidth={Bandwidth}bytes, transforms={Transforms}, expires_at={ExpiresAt}",
            newLease.LeaseId, request.TenantId, request.DataplaneId,
            grantedBandwidth, grantedTransforms, newLease.ExpiresAt);

        return MapToResponse(newLease);
    }

    private async Task<BillingPeriod> GetOrCreateBillingPeriodAsync(
        Guid tenantId,
        Guid periodId,
        CancellationToken cancellationToken)
    {
        var period = await _db.BillingPeriods
            .FirstOrDefaultAsync(p => p.PeriodId == periodId, cancellationToken);

        if (period != null)
        {
            return period;
        }

        // Create new period (typically for current month)
        var now = DateTimeOffset.UtcNow;
        var periodKey = now.ToString("yyyy-MM"); // e.g., "2024-12"

        period = new BillingPeriod
        {
            PeriodId = periodId,
            TenantId = tenantId,
            PeriodKey = periodKey,
            StartsAt = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero),
            EndsAt = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(1),
            BandwidthConsumedBytes = 0,
            TransformsConsumed = 0,
            CreatedAt = now
        };

        _db.BillingPeriods.Add(period);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created billing period: period_id={PeriodId}, tenant_id={TenantId}, period_key={PeriodKey}",
            periodId, tenantId, periodKey);

        return period;
    }

    private static BudgetAllocateResponse MapToResponse(BudgetLease lease)
    {
        return new BudgetAllocateResponse
        {
            LeaseId = lease.LeaseId,
            BandwidthGrantedBytes = lease.BandwidthGrantedBytes,
            TransformsGranted = lease.TransformsGranted,
            GrantedAt = lease.GrantedAt,
            ExpiresAt = lease.ExpiresAt
        };
    }
}
