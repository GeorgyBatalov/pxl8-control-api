namespace Pxl8.ControlApi.Data.Entities;

/// <summary>
/// User entity for JWT authentication
/// </summary>
/// <remarks>
/// Supports two roles: Admin (PXL8 staff) and TenantOwner (customer)
/// Spec: MILESTONE_5_MVP_PLAN.md - Simple JWT Authentication
/// </remarks>
public class User
{
    /// <summary>
    /// User unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User email (unique, used for login)
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// BCrypt password hash
    /// </summary>
    public required string PasswordHash { get; set; }

    /// <summary>
    /// User role (Admin or TenantOwner)
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Tenant ID (null for Admin users, required for TenantOwner)
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// User role enumeration
/// </summary>
public enum UserRole
{
    /// <summary>
    /// PXL8 staff - full access to all tenants and admin endpoints
    /// </summary>
    Admin = 0,

    /// <summary>
    /// Tenant owner - limited access to their own tenant only
    /// </summary>
    TenantOwner = 1
}
