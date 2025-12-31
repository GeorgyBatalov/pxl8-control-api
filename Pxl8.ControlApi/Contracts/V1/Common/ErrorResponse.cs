using System.Text.Json.Serialization;

namespace Pxl8.ControlApi.Contracts.V1.Common;

/// <summary>
/// Unified error response contract (all APIs)
/// </summary>
public record ErrorResponse
{
    [JsonPropertyName("error_code")]
    public required string ErrorCode { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("details")]
    public Dictionary<string, object>? Details { get; init; }

    [JsonPropertyName("trace_id")]
    public required Guid TraceId { get; init; }

    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }
}
