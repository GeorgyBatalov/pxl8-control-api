namespace Pxl8.ControlApi.Data.Entities;

/// <summary>
/// Domain entity for multi-tenant domain management
/// </summary>
/// <remarks>
/// Domains are verified via DNS TXT or HTTP file challenge
/// Spec: MILESTONE_5_MVP_PLAN.md - Domain Management
/// </remarks>
public class Domain
{
    /// <summary>
    /// Domain unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant ID (owner)
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Domain name (e.g., "cdn.example.com")
    /// </summary>
    public required string DomainName { get; set; }

    /// <summary>
    /// Whether domain ownership has been verified
    /// </summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// Verification method (dns or http)
    /// </summary>
    public string? VerificationMethod { get; set; }

    /// <summary>
    /// Token used for DNS TXT or HTTP file verification
    /// </summary>
    public string? VerificationToken { get; set; }

    /// <summary>
    /// When verification was completed (null if not verified)
    /// </summary>
    public DateTimeOffset? VerifiedAt { get; set; }

    /// <summary>
    /// Whether domain is active (soft delete flag)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
