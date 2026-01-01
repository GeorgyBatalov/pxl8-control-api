using System.Text.Json.Serialization;

namespace Pxl8.ControlApi.Contracts.V1.TenantManagement;

public record DomainDto
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("tenant_id")]
    public required Guid TenantId { get; init; }

    [JsonPropertyName("domain_name")]
    public required string DomainName { get; init; }

    [JsonPropertyName("is_verified")]
    public bool IsVerified { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
}

public record AddDomainRequest
{
    [JsonPropertyName("domain_name")]
    public required string DomainName { get; init; }
}

public record ApiKeyDto
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("tenant_id")]
    public required Guid TenantId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; init; }
}

public record GenerateApiKeyRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; init; }
}

public record ApiKeyCreatedResponse
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; init; }
}
