namespace Pxl8.ControlApi.Data.Entities;

/// <summary>
/// Billing period entity
/// </summary>
/// <remarks>
/// One period per tenant per month (typically)
/// Spec: ARCHITECTURE_SPLIT.md - period_id ALWAYS GUID
/// </remarks>
public class BillingPeriod
{
    /// <summary>
    /// Billing period unique identifier (GUID, NOT "YYYY-MM")
    /// </summary>
    public Guid PeriodId { get; set; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Period key for UI/reporting (e.g., "2024-12")
    /// </summary>
    /// <remarks>
    /// Human-readable key, but NOT used as primary identifier
    /// </remarks>
    public required string PeriodKey { get; set; }

    /// <summary>
    /// Period start date (UTC)
    /// </summary>
    public DateTimeOffset StartsAt { get; set; }

    /// <summary>
    /// Period end date (UTC)
    /// </summary>
    public DateTimeOffset EndsAt { get; set; }

    /// <summary>
    /// Total bandwidth consumed in this period (bytes)
    /// </summary>
    /// <remarks>
    /// Aggregate from all UsageReports
    /// </remarks>
    public long BandwidthConsumedBytes { get; set; }

    /// <summary>
    /// Total transforms consumed in this period (count)
    /// </summary>
    public int TransformsConsumed { get; set; }

    /// <summary>
    /// Bandwidth limit for this period (bytes)
    /// </summary>
    public long BandwidthLimit { get; set; }

    /// <summary>
    /// Transforms limit for this period (count)
    /// </summary>
    public int TransformsLimit { get; set; }

    /// <summary>
    /// Storage limit for this period (bytes)
    /// </summary>
    public long StorageLimit { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
