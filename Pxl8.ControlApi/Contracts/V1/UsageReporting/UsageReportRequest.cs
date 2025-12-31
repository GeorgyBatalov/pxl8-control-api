using System.Text.Json.Serialization;

namespace Pxl8.ControlApi.Contracts.V1.UsageReporting;

/// <summary>
/// Usage report from Data Plane to Control Plane
/// </summary>
public record UsageReportRequest
{
    [JsonPropertyName("report_id")]
    public required Guid ReportId { get; init; }

    [JsonPropertyName("dataplane_id")]
    public required string DataplaneId { get; init; }

    [JsonPropertyName("tenant_id")]
    public required Guid TenantId { get; init; }

    [JsonPropertyName("period_id")]
    public required Guid PeriodId { get; init; }

    [JsonPropertyName("bandwidth_used_bytes")]
    public required long BandwidthUsedBytes { get; init; }

    [JsonPropertyName("transforms_used")]
    public required int TransformsUsed { get; init; }

    [JsonPropertyName("reported_at")]
    public required DateTimeOffset ReportedAt { get; init; }
}
