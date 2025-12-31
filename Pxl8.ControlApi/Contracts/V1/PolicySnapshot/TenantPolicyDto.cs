using System.Text.Json.Serialization;

namespace Pxl8.ControlApi.Contracts.V1.PolicySnapshot;

public record TenantPolicyDto
{
    [JsonPropertyName("tenant_id")]
    public required Guid TenantId { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("plan_code")]
    public required string PlanCode { get; init; }

    [JsonPropertyName("quotas")]
    public required QuotasDto Quotas { get; init; }

    [JsonPropertyName("domains")]
    public required IReadOnlyList<DomainDto> Domains { get; init; }

    [JsonPropertyName("api_keys")]
    public required IReadOnlyList<ApiKeyDto> ApiKeys { get; init; }
}

public record QuotasDto
{
    [JsonPropertyName("bandwidth_limit_bytes")]
    public required long BandwidthLimitBytes { get; init; }

    [JsonPropertyName("transforms_limit")]
    public required int TransformsLimit { get; init; }

    [JsonPropertyName("storage_limit_bytes")]
    public required long StorageLimitBytes { get; init; }

    [JsonPropertyName("domains_limit")]
    public required int DomainsLimit { get; init; }
}

public record DomainDto
{
    [JsonPropertyName("domain")]
    public required string Domain { get; init; }

    [JsonPropertyName("verified")]
    public required bool Verified { get; init; }
}

public record ApiKeyDto
{
    [JsonPropertyName("key_prefix")]
    public required string KeyPrefix { get; init; }

    [JsonPropertyName("key_hmac")]
    public required string KeyHmac { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }
}
