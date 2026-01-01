namespace Pxl8.ControlApi.Data.Entities;

/// <summary>
/// Tenant entity - represents a customer account
/// </summary>
/// <remarks>
/// Simplified for MVP - quota management via BillingPeriods
/// Spec: MILESTONE_5_MVP_PLAN.md - Tenant Management API
/// </remarks>
public class Tenant
{
    /// <summary>
    /// Tenant unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant name (company/organization name)
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Contact email for tenant
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Whether tenant is active (suspended = false)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Reason for suspension (if IsActive = false)
    /// </summary>
    public string? SuspensionReason { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
