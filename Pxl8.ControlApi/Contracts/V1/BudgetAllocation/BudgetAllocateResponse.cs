using System.Text.Json.Serialization;

namespace Pxl8.ControlApi.Contracts.V1.BudgetAllocation;

/// <summary>
/// Response from Control Plane with budget lease
/// </summary>
public record BudgetAllocateResponse
{
    [JsonPropertyName("lease_id")]
    public required Guid LeaseId { get; init; }

    [JsonPropertyName("bandwidth_granted_bytes")]
    public required long BandwidthGrantedBytes { get; init; }

    [JsonPropertyName("transforms_granted")]
    public required int TransformsGranted { get; init; }

    [JsonPropertyName("granted_at")]
    public required DateTimeOffset GrantedAt { get; init; }

    [JsonPropertyName("expires_at")]
    public required DateTimeOffset ExpiresAt { get; init; }
}
