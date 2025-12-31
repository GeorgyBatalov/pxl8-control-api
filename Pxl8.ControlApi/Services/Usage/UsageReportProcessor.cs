using Microsoft.EntityFrameworkCore;
using Pxl8.ControlApi.Contracts.V1.UsageReporting;
using Pxl8.ControlApi.Data;
using Pxl8.ControlApi.Data.Entities;

namespace Pxl8.ControlApi.Services.Usage;

/// <summary>
/// Usage report processor service - process usage reports from Data Planes
/// </summary>
public class UsageReportProcessor : IUsageReportProcessor
{
    private readonly ControlDbContext _db;
    private readonly ILogger<UsageReportProcessor> _logger;

    public UsageReportProcessor(ControlDbContext db, ILogger<UsageReportProcessor> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<UsageReportResponse> ProcessReportAsync(
        UsageReportRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Check idempotency - if report_id exists, return success (no-op)
        var existingReport = await _db.UsageReports
            .FirstOrDefaultAsync(r => r.ReportId == request.ReportId, cancellationToken);

        if (existingReport != null)
        {
            _logger.LogInformation(
                "Idempotent request: report {ReportId} already processed (received at {ReceivedAt})",
                request.ReportId, existingReport.ReceivedAt);

            // Get current totals from billing period
            var existingPeriod = await _db.BillingPeriods
                .FirstOrDefaultAsync(p => p.PeriodId == request.PeriodId, cancellationToken);

            return new UsageReportResponse
            {
                Accepted = true, // Idempotent - already processed
                TotalBandwidthBytes = existingPeriod?.BandwidthConsumedBytes ?? 0,
                TotalTransforms = existingPeriod?.TransformsConsumed ?? 0
            };
        }

        // 2. Save usage report to database
        var now = DateTimeOffset.UtcNow;
        var report = new UsageReport
        {
            ReportId = request.ReportId,
            DataplaneId = request.DataplaneId,
            TenantId = request.TenantId,
            PeriodId = request.PeriodId,
            BandwidthUsedBytes = request.BandwidthUsedBytes,
            TransformsUsed = request.TransformsUsed,
            ReportedAt = request.ReportedAt,
            ReceivedAt = now
        };

        _db.UsageReports.Add(report);

        // 3. Update BillingPeriod consumed counters (atomic)
        var period = await _db.BillingPeriods
            .FirstOrDefaultAsync(p => p.PeriodId == request.PeriodId, cancellationToken);

        if (period != null)
        {
            period.BandwidthConsumedBytes += request.BandwidthUsedBytes;
            period.TransformsConsumed += request.TransformsUsed;

            _logger.LogInformation(
                "Updated billing period {PeriodId}: bandwidth={BandwidthConsumed}bytes, transforms={TransformsConsumed}",
                period.PeriodId, period.BandwidthConsumedBytes, period.TransformsConsumed);
        }
        else
        {
            _logger.LogWarning(
                "Billing period {PeriodId} not found - creating new period for tenant {TenantId}",
                request.PeriodId, request.TenantId);

            // Create new period if not exists (edge case - should normally exist)
            var periodKey = request.ReportedAt.ToString("yyyy-MM");
            var newPeriod = new BillingPeriod
            {
                PeriodId = request.PeriodId,
                TenantId = request.TenantId,
                PeriodKey = periodKey,
                StartsAt = new DateTimeOffset(request.ReportedAt.Year, request.ReportedAt.Month, 1, 0, 0, 0, TimeSpan.Zero),
                EndsAt = new DateTimeOffset(request.ReportedAt.Year, request.ReportedAt.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(1),
                BandwidthConsumedBytes = request.BandwidthUsedBytes,
                TransformsConsumed = request.TransformsUsed,
                CreatedAt = now
            };

            _db.BillingPeriods.Add(newPeriod);
        }

        // 4. Save changes atomically
        await _db.SaveChangesAsync(cancellationToken);

        // Get updated period for totals
        var updatedPeriod = await _db.BillingPeriods
            .FirstOrDefaultAsync(p => p.PeriodId == request.PeriodId, cancellationToken);

        _logger.LogInformation(
            "Usage report processed: report_id={ReportId}, dataplane={DataplaneId}, tenant={TenantId}, " +
            "bandwidth={Bandwidth}bytes, transforms={Transforms}, " +
            "total_bandwidth={TotalBandwidth}bytes, total_transforms={TotalTransforms}",
            request.ReportId, request.DataplaneId, request.TenantId,
            request.BandwidthUsedBytes, request.TransformsUsed,
            updatedPeriod?.BandwidthConsumedBytes ?? 0, updatedPeriod?.TransformsConsumed ?? 0);

        return new UsageReportResponse
        {
            Accepted = true,
            TotalBandwidthBytes = updatedPeriod?.BandwidthConsumedBytes ?? 0,
            TotalTransforms = updatedPeriod?.TransformsConsumed ?? 0
        };
    }
}
