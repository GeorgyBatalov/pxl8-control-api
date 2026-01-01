using System.Text.Json.Serialization;

namespace Pxl8.ControlApi.Contracts.V1.TenantManagement;

/// <summary>
/// Billing period response DTO
/// </summary>
public record BillingPeriodDto
{
    [JsonPropertyName("period_id")]
    public required Guid PeriodId { get; init; }

    [JsonPropertyName("tenant_id")]
    public required Guid TenantId { get; init; }

    [JsonPropertyName("period_key")]
    public required string PeriodKey { get; init; }

    [JsonPropertyName("starts_at")]
    public DateTimeOffset StartsAt { get; init; }

    [JsonPropertyName("ends_at")]
    public DateTimeOffset EndsAt { get; init; }

    [JsonPropertyName("bandwidth_limit")]
    public long BandwidthLimit { get; init; }

    [JsonPropertyName("transforms_limit")]
    public int TransformsLimit { get; init; }

    [JsonPropertyName("storage_limit")]
    public long StorageLimit { get; init; }

    [JsonPropertyName("bandwidth_consumed")]
    public long BandwidthConsumed { get; init; }

    [JsonPropertyName("transforms_consumed")]
    public int TransformsConsumed { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Create billing period request DTO
/// </summary>
public record CreateBillingPeriodRequest
{
    [JsonPropertyName("period_key")]
    public required string PeriodKey { get; init; }

    [JsonPropertyName("starts_at")]
    public DateTimeOffset StartsAt { get; init; }

    [JsonPropertyName("ends_at")]
    public DateTimeOffset EndsAt { get; init; }

    [JsonPropertyName("bandwidth_limit")]
    public long BandwidthLimit { get; init; }

    [JsonPropertyName("transforms_limit")]
    public int TransformsLimit { get; init; }

    [JsonPropertyName("storage_limit")]
    public long StorageLimit { get; init; }
}

/// <summary>
/// Update billing period request DTO
/// </summary>
public record UpdateBillingPeriodRequest
{
    [JsonPropertyName("bandwidth_limit")]
    public long? BandwidthLimit { get; init; }

    [JsonPropertyName("transforms_limit")]
    public int? TransformsLimit { get; init; }

    [JsonPropertyName("storage_limit")]
    public long? StorageLimit { get; init; }
}
