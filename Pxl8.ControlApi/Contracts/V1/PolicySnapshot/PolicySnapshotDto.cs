using System.Text.Json.Serialization;
using Pxl8.ControlApi.Contracts.Abstractions;

namespace Pxl8.ControlApi.Contracts.V1.PolicySnapshot;

/// <summary>
/// Policy snapshot - atomic configuration bundle for Data Plane
/// </summary>
public record PolicySnapshotDto : IVersioned
{
    [JsonPropertyName("snapshot_id")]
    public required Guid SnapshotId { get; init; }

    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;

    [JsonPropertyName("generated_at")]
    public required DateTimeOffset GeneratedAt { get; init; }

    [JsonPropertyName("tenants")]
    public required IReadOnlyList<TenantPolicyDto> Tenants { get; init; }
}
