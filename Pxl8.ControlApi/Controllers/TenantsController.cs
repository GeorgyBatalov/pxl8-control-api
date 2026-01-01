using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pxl8.ControlApi.Contracts.V1.Common;
using Pxl8.ControlApi.Contracts.V1.TenantManagement;
using Pxl8.ControlApi.Data;
using Pxl8.ControlApi.Data.Entities;

namespace Pxl8.ControlApi.Controllers;

/// <summary>
/// Tenant Management API
/// </summary>
[ApiController]
[Route("api/tenants")]
[Authorize] // Require JWT authentication
public class TenantsController : ControllerBase
{
    private readonly ControlDbContext _db;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(ControlDbContext db, ILogger<TenantsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/tenants - List all tenants
    /// </summary>
    /// <remarks>
    /// Admin: Returns all tenants
    /// TenantOwner: Returns only their tenant
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<TenantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTenants(CancellationToken cancellationToken)
    {
        var userRole = User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
        var userTenantId = User.FindFirst("TenantId")?.Value;

        IQueryable<Tenant> query = _db.Tenants;

        // TenantOwner can only see their own tenant
        if (userRole == "TenantOwner" && userTenantId != null)
        {
            query = query.Where(t => t.Id == Guid.Parse(userTenantId));
        }

        var tenants = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                Email = t.Email,
                IsActive = t.IsActive,
                SuspensionReason = t.SuspensionReason,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(tenants);
    }

    /// <summary>
    /// GET /api/tenants/{id} - Get tenant details
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTenant(Guid id, CancellationToken cancellationToken)
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

        // TenantOwner can only access their own tenant
        var userRole = User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
        var userTenantId = User.FindFirst("TenantId")?.Value;

        if (userRole == "TenantOwner" && userTenantId != id.ToString())
        {
            return StatusCode(403, new ErrorResponse
            {
                ErrorCode = "FORBIDDEN",
                Message = "You can only access your own tenant",
                TraceId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        return Ok(new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Email = tenant.Email,
            IsActive = tenant.IsActive,
            SuspensionReason = tenant.SuspensionReason,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        });
    }

    /// <summary>
    /// POST /api/tenants - Create tenant
    /// </summary>
    /// <remarks>
    /// Admin only
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "Admin")] // Only Admin can create tenants
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTenant(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "INVALID_REQUEST",
                Message = "Name and email are required",
                TraceId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        // Create tenant
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tenant created. TenantId: {TenantId}, Name: {Name}", tenant.Id, tenant.Name);

        var dto = new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Email = tenant.Email,
            IsActive = tenant.IsActive,
            SuspensionReason = tenant.SuspensionReason,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };

        return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, dto);
    }

    /// <summary>
    /// PUT /api/tenants/{id} - Update tenant
    /// </summary>
    /// <remarks>
    /// Admin: Can update any tenant
    /// TenantOwner: Can only update their own tenant (name/email only)
    /// </remarks>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateTenant(
        Guid id,
        [FromBody] UpdateTenantRequest request,
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

        // TenantOwner can only update their own tenant
        var userRole = User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
        var userTenantId = User.FindFirst("TenantId")?.Value;

        if (userRole == "TenantOwner" && userTenantId != id.ToString())
        {
            return StatusCode(403, new ErrorResponse
            {
                ErrorCode = "FORBIDDEN",
                Message = "You can only update your own tenant",
                TraceId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        // Update fields
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            tenant.Name = request.Name;
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            tenant.Email = request.Email;
        }

        tenant.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tenant updated. TenantId: {TenantId}", tenant.Id);

        return Ok(new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Email = tenant.Email,
            IsActive = tenant.IsActive,
            SuspensionReason = tenant.SuspensionReason,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        });
    }

    /// <summary>
    /// DELETE /api/tenants/{id} - Delete tenant (soft delete)
    /// </summary>
    /// <remarks>
    /// Admin only - sets IsActive = false
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")] // Only Admin can delete tenants
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTenant(Guid id, CancellationToken cancellationToken)
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

        // Soft delete
        tenant.IsActive = false;
        tenant.SuspensionReason = "Deleted by admin";
        tenant.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Tenant deleted (soft). TenantId: {TenantId}", tenant.Id);

        return NoContent();
    }
}
