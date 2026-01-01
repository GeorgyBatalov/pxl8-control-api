using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pxl8.ControlApi.Contracts.V1.Common;
using Pxl8.ControlApi.Contracts.V1.TenantManagement;
using Pxl8.ControlApi.Data;
using Pxl8.ControlApi.Data.Entities;

namespace Pxl8.ControlApi.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/domains")]
[Authorize]
public class DomainsController : ControllerBase
{
    private readonly ControlDbContext _db;

    public DomainsController(ControlDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<DomainDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListDomains(Guid tenantId, CancellationToken cancellationToken)
    {
        var domains = await _db.Domains
            .Where(d => d.TenantId == tenantId && d.IsActive)
            .Select(d => new DomainDto
            {
                Id = d.Id,
                TenantId = d.TenantId,
                DomainName = d.DomainName,
                IsVerified = d.IsVerified,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(domains);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DomainDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddDomain(
        Guid tenantId,
        [FromBody] AddDomainRequest request,
        CancellationToken cancellationToken)
    {
        var domain = new Domain
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DomainName = request.DomainName,
            IsVerified = false, // MVP: no verification
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Domains.Add(domain);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(ListDomains), new { tenantId }, new DomainDto
        {
            Id = domain.Id,
            TenantId = domain.TenantId,
            DomainName = domain.DomainName,
            IsVerified = domain.IsVerified,
            CreatedAt = domain.CreatedAt
        });
    }

    [HttpDelete("{domainId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveDomain(
        Guid tenantId,
        Guid domainId,
        CancellationToken cancellationToken)
    {
        var domain = await _db.Domains
            .FirstOrDefaultAsync(d => d.Id == domainId && d.TenantId == tenantId, cancellationToken);

        if (domain == null)
            return NotFound();

        domain.IsActive = false; // Soft delete
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
