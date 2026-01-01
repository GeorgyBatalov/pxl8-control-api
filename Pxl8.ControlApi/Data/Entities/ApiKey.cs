namespace Pxl8.ControlApi.Data.Entities;

/// <summary>
/// API Key entity for tenant authentication
/// </summary>
/// <remarks>
/// Keys are hashed (SHA256) for secure storage
/// Spec: MILESTONE_5_MVP_PLAN.md - API Key Management
/// </remarks>
public class ApiKey
{
    /// <summary>
    /// API key unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant ID (owner)
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// SHA256 hash of the API key (not plain text)
    /// </summary>
    public required string KeyHash { get; set; }

    /// <summary>
    /// User-friendly name for the API key
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Whether the API key is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last time this API key was used successfully
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the API key expires (optional, null = never)
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
