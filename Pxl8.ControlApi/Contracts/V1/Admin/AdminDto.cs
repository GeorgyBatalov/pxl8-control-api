using System.Text.Json.Serialization;

namespace Pxl8.ControlApi.Contracts.V1.Admin;

/// <summary>
/// Suspend tenant request DTO
/// </summary>
public record SuspendTenantRequest
{
    [JsonPropertyName("reason")]
    public required string Reason { get; init; }
}

/// <summary>
/// Override quota request DTO
/// </summary>
public record OverrideQuotaRequest
{
    [JsonPropertyName("quota_type")]
    public required string QuotaType { get; init; } // "bandwidth", "transforms", "storage"

    [JsonPropertyName("new_limit")]
    public long NewLimit { get; init; }

    [JsonPropertyName("reason")]
    public required string Reason { get; init; }
}

/// <summary>
/// Usage report summary DTO
/// </summary>
public record UsageReportSummaryDto
{
    [JsonPropertyName("report_id")]
    public required Guid ReportId { get; init; }

    [JsonPropertyName("dataplane_id")]
    public required string DataplaneId { get; init; }

    [JsonPropertyName("tenant_id")]
    public required Guid TenantId { get; init; }

    [JsonPropertyName("period_id")]
    public required Guid PeriodId { get; init; }

    [JsonPropertyName("bandwidth_used")]
    public long BandwidthUsed { get; init; }

    [JsonPropertyName("transforms_used")]
    public int TransformsUsed { get; init; }

    [JsonPropertyName("reported_at")]
    public DateTimeOffset ReportedAt { get; init; }

    [JsonPropertyName("received_at")]
    public DateTimeOffset ReceivedAt { get; init; }
}

/// <summary>
/// Budget lease summary DTO
/// </summary>
public record BudgetLeaseSummaryDto
{
    [JsonPropertyName("lease_id")]
    public required Guid LeaseId { get; init; }

    [JsonPropertyName("tenant_id")]
    public required Guid TenantId { get; init; }

    [JsonPropertyName("period_id")]
    public required Guid PeriodId { get; init; }

    [JsonPropertyName("dataplane_id")]
    public required string DataplaneId { get; init; }

    [JsonPropertyName("bandwidth_granted")]
    public long BandwidthGranted { get; init; }

    [JsonPropertyName("transforms_granted")]
    public int TransformsGranted { get; init; }

    [JsonPropertyName("granted_at")]
    public DateTimeOffset GrantedAt { get; init; }

    [JsonPropertyName("expires_at")]
    public DateTimeOffset ExpiresAt { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }
}
