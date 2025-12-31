namespace Pxl8.ControlApi.Data.Entities;

/// <summary>
/// Budget lease entity (issued to Data Planes)
/// </summary>
/// <remarks>
/// ONE active lease per (tenant_id, period_id, dataplane_id) tuple
/// Spec: BUDGET_ALGORITHM.md v1.1, ARCHITECTURE_SPLIT.md v1.2
/// </remarks>
public class BudgetLease
{
    /// <summary>
    /// Unique lease identifier
    /// </summary>
    public Guid LeaseId { get; set; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Billing period ID (GUID)
    /// </summary>
    public Guid PeriodId { get; set; }

    /// <summary>
    /// Data Plane identifier (e.g., "ru-central1-a")
    /// </summary>
    public required string DataplaneId { get; set; }

    /// <summary>
    /// Bandwidth granted in this lease (bytes)
    /// </summary>
    public long BandwidthGrantedBytes { get; set; }

    /// <summary>
    /// Transforms granted in this lease (count)
    /// </summary>
    public int TransformsGranted { get; set; }

    /// <summary>
    /// Lease grant timestamp (UTC)
    /// </summary>
    public DateTimeOffset GrantedAt { get; set; }

    /// <summary>
    /// Lease expiry timestamp (UTC)
    /// </summary>
    /// <remarks>
    /// Typically GrantedAt + 5 minutes (TTL)
    /// </remarks>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Lease status (Active | Expired | Revoked)
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Revoked timestamp (UTC, nullable)
    /// </summary>
    /// <remarks>
    /// Set when lease is revoked (new allocation for same tuple)
    /// </remarks>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// Idempotency key (request_id from allocation request)
    /// </summary>
    /// <remarks>
    /// UNIQUE constraint - same request_id returns same lease
    /// </remarks>
    public Guid RequestId { get; set; }
}
