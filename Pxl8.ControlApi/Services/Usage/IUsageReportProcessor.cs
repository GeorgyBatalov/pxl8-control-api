using Pxl8.ControlApi.Contracts.V1.UsageReporting;

namespace Pxl8.ControlApi.Services.Usage;

/// <summary>
/// Usage report processor service - process usage reports from Data Planes
/// </summary>
public interface IUsageReportProcessor
{
    /// <summary>
    /// Process usage report from Data Plane
    /// </summary>
    /// <remarks>
    /// Idempotent by report_id - duplicate reports are ignored
    /// Updates BillingPeriod consumed counters atomically
    /// </remarks>
    Task<UsageReportResponse> ProcessReportAsync(UsageReportRequest request, CancellationToken cancellationToken = default);
}
