namespace Pxl8.ControlApi.Data.Entities;

/// <summary>
/// Usage report entity (from Data Planes)
/// </summary>
/// <remarks>
/// Idempotent by report_id - duplicates ignored
/// Spec: ARCHITECTURE_SPLIT.md v1.2
/// </remarks>
public class UsageReport
{
    /// <summary>
    /// Unique report identifier (for idempotency)
    /// </summary>
    /// <remarks>
    /// UNIQUE constraint - duplicate report_id = no-op
    /// </remarks>
    public Guid ReportId { get; set; }

    /// <summary>
    /// Data Plane identifier (e.g., "ru-central1-a")
    /// </summary>
    public required string DataplaneId { get; set; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Billing period ID (GUID)
    /// </summary>
    public Guid PeriodId { get; set; }

    /// <summary>
    /// Bandwidth consumed (bytes, delta)
    /// </summary>
    /// <remarks>
    /// Delta since last report, not cumulative
    /// </remarks>
    public long BandwidthUsedBytes { get; set; }

    /// <summary>
    /// Transforms consumed (count, delta)
    /// </summary>
    public int TransformsUsed { get; set; }

    /// <summary>
    /// Report timestamp (from Data Plane)
    /// </summary>
    public DateTimeOffset ReportedAt { get; set; }

    /// <summary>
    /// Received timestamp (Control Plane)
    /// </summary>
    public DateTimeOffset ReceivedAt { get; set; }
}
