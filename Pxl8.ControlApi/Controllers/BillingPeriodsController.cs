using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pxl8.ControlApi.Contracts.V1.Common;
using Pxl8.ControlApi.Contracts.V1.TenantManagement;
using Pxl8.ControlApi.Data;
using Pxl8.ControlApi.Data.Entities;

namespace Pxl8.ControlApi.Controllers;

/// <summary>
/// Billing Periods Management API
/// </summary>
[ApiController]
[Route("api/tenants/{tenantId:guid}/billing-periods")]
[Authorize] // Require JWT authentication
public class BillingPeriodsController : ControllerBase
{
    private readonly ControlDbContext _db;
    private readonly ILogger<BillingPeriodsController> _logger;

    public BillingPeriodsController(ControlDbContext db, ILogger<BillingPeriodsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/tenants/{tenantId}/billing-periods - List billing periods
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<BillingPeriodDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListBillingPeriods(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var periods = await _db.BillingPeriods
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.StartsAt)
            .Select(p => new BillingPeriodDto
            {
                PeriodId = p.PeriodId,
                TenantId = p.TenantId,
                PeriodKey = p.PeriodKey,
                StartsAt = p.StartsAt,
                EndsAt = p.EndsAt,
                BandwidthLimit = p.BandwidthLimit,
                TransformsLimit = p.TransformsLimit,
                StorageLimit = p.StorageLimit,
                BandwidthConsumed = p.BandwidthConsumedBytes,
                TransformsConsumed = p.TransformsConsumed,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(periods);
    }

    /// <summary>
    /// POST /api/tenants/{tenantId}/billing-periods - Create billing period
    /// </summary>
    /// <remarks>
    /// Admin only - sets quotas for tenant
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "Admin")] // Only Admin can create billing periods
    [ProducesResponseType(typeof(BillingPeriodDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBillingPeriod(
        Guid tenantId,
        [FromBody] CreateBillingPeriodRequest request,
        CancellationToken cancellationToken)
    {
        // Validate tenant exists
        var tenantExists = await _db.Tenants.AnyAsync(t => t.Id == tenantId, cancellationToken);
        if (!tenantExists)
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "TENANT_NOT_FOUND",
                Message = $"Tenant {tenantId} not found",
                TraceId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        // Create billing period
        var period = new BillingPeriod
        {
            PeriodId = Guid.NewGuid(),
            TenantId = tenantId,
            PeriodKey = request.PeriodKey,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            BandwidthLimit = request.BandwidthLimit,
            TransformsLimit = request.TransformsLimit,
            StorageLimit = request.StorageLimit,
            BandwidthConsumedBytes = 0,
            TransformsConsumed = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.BillingPeriods.Add(period);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Billing period created. TenantId: {TenantId}, PeriodId: {PeriodId}, PeriodKey: {PeriodKey}",
            tenantId, period.PeriodId, period.PeriodKey);

        var dto = new BillingPeriodDto
        {
            PeriodId = period.PeriodId,
            TenantId = period.TenantId,
            PeriodKey = period.PeriodKey,
            StartsAt = period.StartsAt,
            EndsAt = period.EndsAt,
            BandwidthLimit = period.BandwidthLimit,
            TransformsLimit = period.TransformsLimit,
            StorageLimit = period.StorageLimit,
            BandwidthConsumed = period.BandwidthConsumedBytes,
            TransformsConsumed = period.TransformsConsumed,
            CreatedAt = period.CreatedAt
        };

        return CreatedAtAction(nameof(ListBillingPeriods), new { tenantId }, dto);
    }

    /// <summary>
    /// PUT /api/tenants/{tenantId}/billing-periods/{periodId} - Update billing period quotas
    /// </summary>
    /// <remarks>
    /// Admin only - allows emergency quota override
    /// </remarks>
    [HttpPut("{periodId:guid}")]
    [Authorize(Roles = "Admin")] // Only Admin can update quotas
    [ProducesResponseType(typeof(BillingPeriodDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBillingPeriod(
        Guid tenantId,
        Guid periodId,
        [FromBody] UpdateBillingPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var period = await _db.BillingPeriods
            .FirstOrDefaultAsync(p => p.PeriodId == periodId && p.TenantId == tenantId, cancellationToken);

        if (period == null)
        {
            return NotFound(new ErrorResponse
            {
                ErrorCode = "BILLING_PERIOD_NOT_FOUND",
                Message = $"Billing period {periodId} not found for tenant {tenantId}",
                TraceId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        // Update quotas if provided
        if (request.BandwidthLimit.HasValue)
        {
            period.BandwidthLimit = request.BandwidthLimit.Value;
        }

        if (request.TransformsLimit.HasValue)
        {
            period.TransformsLimit = request.TransformsLimit.Value;
        }

        if (request.StorageLimit.HasValue)
        {
            period.StorageLimit = request.StorageLimit.Value;
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Billing period quotas updated. TenantId: {TenantId}, PeriodId: {PeriodId}",
            tenantId, periodId);

        return Ok(new BillingPeriodDto
        {
            PeriodId = period.PeriodId,
            TenantId = period.TenantId,
            PeriodKey = period.PeriodKey,
            StartsAt = period.StartsAt,
            EndsAt = period.EndsAt,
            BandwidthLimit = period.BandwidthLimit,
            TransformsLimit = period.TransformsLimit,
            StorageLimit = period.StorageLimit,
            BandwidthConsumed = period.BandwidthConsumedBytes,
            TransformsConsumed = period.TransformsConsumed,
            CreatedAt = period.CreatedAt
        });
    }
}
