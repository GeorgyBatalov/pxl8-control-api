using System.Text.Json.Serialization;

namespace Pxl8.ControlApi.Contracts.V1.UsageReporting;

/// <summary>
/// Response from Control Plane after processing usage report
/// </summary>
public record UsageReportResponse
{
    [JsonPropertyName("accepted")]
    public required bool Accepted { get; init; }

    [JsonPropertyName("total_bandwidth_bytes")]
    public required long TotalBandwidthBytes { get; init; }

    [JsonPropertyName("total_transforms")]
    public required int TotalTransforms { get; init; }
}
