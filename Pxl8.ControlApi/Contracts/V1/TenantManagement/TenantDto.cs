using System.Text.Json.Serialization;

namespace Pxl8.ControlApi.Contracts.V1.TenantManagement;

/// <summary>
/// Tenant response DTO
/// </summary>
public record TenantDto
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; }

    [JsonPropertyName("suspension_reason")]
    public string? SuspensionReason { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Create tenant request DTO
/// </summary>
public record CreateTenantRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }
}

/// <summary>
/// Update tenant request DTO
/// </summary>
public record UpdateTenantRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }
}
