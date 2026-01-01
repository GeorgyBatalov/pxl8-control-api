using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pxl8.ControlApi.Contracts.V1.Admin;
using Pxl8.ControlApi.Contracts.V1.Common;
using Pxl8.ControlApi.Data;

namespace Pxl8.ControlApi.Controllers;

/// <summary>
/// Admin Tools API - PXL8 staff operations
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")] // All endpoints require Admin role
public class AdminController : ControllerBase
{
    private readonly ControlDbContext _db;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ControlDbContext db, ILogger<AdminController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/admin/tenants/{id}/suspend - Suspend tenant
    /// </summary>
    /// <remarks>
    /// Deactivates tenant immediately (sets is_active = false)
    /// Use for abuse detection, non-payment, ToS violations
    /// </remarks>
    [HttpPost("tenants/{id:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuspendTenant(
        Guid id,
        [FromBody] SuspendTenantRequest request,
        CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.FindAsync(new object[] { id }, cancellationToken);

        if (tenant == null)
        {
            return NotFound(new ErrorResponse
            {
                ErrorCode = "TENANT_NOT_FOUND",
                Message = $"Tenant {id} not found",
                TraceId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        tenant.IsActive = false;
        tenant.SuspensionReason = request.Reason;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Tenant suspended. TenantId: {TenantId}, Reason: {Reason}",
            id, request.Reason);

        return Ok(new
        {
            message = "Tenant suspended successfully",
            tenant_id = id,
            reason = request.Reason
        });
    }

    /// <summary>
    /// POST /api/admin/tenants/{id}/resume - Resume tenant
    /// </summary>
    /// <remarks>
    /// Re-activates suspended tenant (sets is_active = true)
    /// </remarks>
    [HttpPost("tenants/{id:guid}/resume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResumeTenant(
        Guid id,
        CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.FindAsync(new object[] { id }, cancellationToken);

        if (tenant == null)
        {
            return NotFound(new ErrorResponse
            {
                ErrorCode = "TENANT_NOT_FOUND",
                Message = $"Tenant {id} not found",
                TraceId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        tenant.IsActive = true;
        tenant.SuspensionReason = null;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tenant resumed. TenantId: {TenantId}", id);

        return Ok(new
        {
            message = "Tenant resumed successfully",
            tenant_id = id
        });
    }

    /// <summary>
    /// POST /api/admin/tenants/{id}/override-quota - Emergency quota override
    /// </summary>
    /// <remarks>
    /// Increases quota limit for current billing period
    /// Use for viral events, TechCrunch features, CEO approvals
    /// </remarks>
    [HttpPost("tenants/{id:guid}/override-quota")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OverrideQuota(
        Guid id,
        [FromBody] OverrideQuotaRequest request,
        CancellationToken cancellationToken)
    {
        // Find active billing period for tenant
        var now = DateTimeOffset.UtcNow;
        var period = await _db.BillingPeriods
            .Where(p => p.TenantId == id && p.StartsAt <= now && p.EndsAt >= now)
            .FirstOrDefaultAsync(cancellationToken);

        if (period == null)
        {
            return NotFound(new ErrorResponse
            {
                ErrorCode = "NO_ACTIVE_PERIOD",
                Message = $"No active billing period found for tenant {id}",
                TraceId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        // Update quota based on type
        switch (request.QuotaType.ToLowerInvariant())
        {
            case "bandwidth":
                period.BandwidthLimit = request.NewLimit;
                break;
            case "transforms":
                period.TransformsLimit = (int)request.NewLimit;
                break;
            case "storage":
                period.StorageLimit = request.NewLimit;
                break;
            default:
                return BadRequest(new ErrorResponse
                {
                    ErrorCode = "INVALID_QUOTA_TYPE",
                    Message = "quota_type must be 'bandwidth', 'transforms', or 'storage'",
                    TraceId = Guid.NewGuid(),
                    Timestamp = DateTimeOffset.UtcNow
                });
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Quota overridden. TenantId: {TenantId}, QuotaType: {QuotaType}, NewLimit: {NewLimit}, Reason: {Reason}",
            id, request.QuotaType, request.NewLimit, request.Reason);

        return Ok(new
        {
            message = "Quota overridden successfully",
            tenant_id = id,
            period_id = period.PeriodId,
            quota_type = request.QuotaType,
            new_limit = request.NewLimit,
            reason = request.Reason
        });
    }

    /// <summary>
    /// GET /api/admin/usage-reports - View usage reports
    /// </summary>
    /// <remarks>
    /// Query usage reports with optional filters
    /// </remarks>
    [HttpGet("usage-reports")]
    [ProducesResponseType(typeof(List<UsageReportSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsageReports(
        [FromQuery] Guid? tenantId,
        [FromQuery] string? dataplaneId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var query = _db.UsageReports.AsQueryable();

        // Apply filters
        if (tenantId.HasValue)
        {
            query = query.Where(r => r.TenantId == tenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(dataplaneId))
        {
            query = query.Where(r => r.DataplaneId == dataplaneId);
        }

        if (from.HasValue)
        {
            query = query.Where(r => r.ReceivedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(r => r.ReceivedAt <= to.Value);
        }

        var reports = await query
            .OrderByDescending(r => r.ReceivedAt)
            .Take(Math.Min(limit ?? 100, 1000)) // Max 1000 results, default 100
            .Select(r => new UsageReportSummaryDto
            {
                ReportId = r.ReportId,
                DataplaneId = r.DataplaneId,
                TenantId = r.TenantId,
                PeriodId = r.PeriodId,
                BandwidthUsed = r.BandwidthUsedBytes,
                TransformsUsed = r.TransformsUsed,
                ReportedAt = r.ReportedAt,
                ReceivedAt = r.ReceivedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(reports);
    }

    /// <summary>
    /// GET /api/admin/budget-leases - View budget leases
    /// </summary>
    /// <remarks>
    /// Query budget leases with optional filters
    /// Useful for debugging Data Gateway budget allocation
    /// </remarks>
    [HttpGet("budget-leases")]
    [ProducesResponseType(typeof(List<BudgetLeaseSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBudgetLeases(
        [FromQuery] Guid? tenantId,
        [FromQuery] string? dataplaneId,
        [FromQuery] string? status,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var query = _db.BudgetLeases.AsQueryable();

        // Apply filters
        if (tenantId.HasValue)
        {
            query = query.Where(l => l.TenantId == tenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(dataplaneId))
        {
            query = query.Where(l => l.DataplaneId == dataplaneId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }

        var leases = await query
            .OrderByDescending(l => l.GrantedAt)
            .Take(Math.Min(limit ?? 100, 1000)) // Max 1000 results, default 100
            .Select(l => new BudgetLeaseSummaryDto
            {
                LeaseId = l.LeaseId,
                TenantId = l.TenantId,
                PeriodId = l.PeriodId,
                DataplaneId = l.DataplaneId,
                BandwidthGranted = l.BandwidthGrantedBytes,
                TransformsGranted = l.TransformsGranted,
                GrantedAt = l.GrantedAt,
                ExpiresAt = l.ExpiresAt,
                Status = l.Status
            })
            .ToListAsync(cancellationToken);

        return Ok(leases);
    }
}
