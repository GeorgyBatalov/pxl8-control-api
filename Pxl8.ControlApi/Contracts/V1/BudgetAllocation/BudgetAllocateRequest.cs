using System.Text.Json.Serialization;

namespace Pxl8.ControlApi.Contracts.V1.BudgetAllocation;

/// <summary>
/// Request to allocate budget lease from Control Plane
/// </summary>
public record BudgetAllocateRequest
{
    [JsonPropertyName("request_id")]
    public required Guid RequestId { get; init; }

    [JsonPropertyName("dataplane_id")]
    public required string DataplaneId { get; init; }

    [JsonPropertyName("tenant_id")]
    public required Guid TenantId { get; init; }

    [JsonPropertyName("period_id")]
    public required Guid PeriodId { get; init; }

    [JsonPropertyName("bandwidth_requested_bytes")]
    public required long BandwidthRequestedBytes { get; init; }

    [JsonPropertyName("transforms_requested")]
    public required int TransformsRequested { get; init; }
}
