using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pxl8.ControlApi.Contracts.V1.TenantManagement;
using Pxl8.ControlApi.Data;
using Pxl8.ControlApi.Data.Entities;

namespace Pxl8.ControlApi.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/api-keys")]
[Authorize]
public class ApiKeysController : ControllerBase
{
    private readonly ControlDbContext _db;

    public ApiKeysController(ControlDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ApiKeyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListApiKeys(Guid tenantId, CancellationToken cancellationToken)
    {
        var keys = await _db.ApiKeys
            .Where(k => k.TenantId == tenantId && k.IsActive)
            .Select(k => new ApiKeyDto
            {
                Id = k.Id,
                TenantId = k.TenantId,
                Name = k.Name,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt,
                ExpiresAt = k.ExpiresAt
            })
            .ToListAsync(cancellationToken);

        return Ok(keys);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiKeyCreatedResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> GenerateApiKey(
        Guid tenantId,
        [FromBody] GenerateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        // Generate random API key (pxl8_live_xxx format)
        var keyBytes = RandomNumberGenerator.GetBytes(32);
        var keyString = $"pxl8_live_{Convert.ToBase64String(keyBytes).Replace("+", "").Replace("/", "").Replace("=", "")}";

        // Hash the key with SHA256
        var keyHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(keyString)));

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            KeyHash = keyHash,
            Name = request.Name,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = request.ExpiresAt
        };

        _db.ApiKeys.Add(apiKey);
        await _db.SaveChangesAsync(cancellationToken);

        // Return plain key ONLY in creation response (not stored)
        return CreatedAtAction(nameof(ListApiKeys), new { tenantId }, new ApiKeyCreatedResponse
        {
            Id = apiKey.Id,
            Key = keyString, // Plain key returned only once!
            Name = apiKey.Name,
            ExpiresAt = apiKey.ExpiresAt
        });
    }

    [HttpDelete("{keyId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeApiKey(
        Guid tenantId,
        Guid keyId,
        CancellationToken cancellationToken)
    {
        var apiKey = await _db.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == keyId && k.TenantId == tenantId, cancellationToken);

        if (apiKey == null)
            return NotFound();

        apiKey.IsActive = false; // Revoke
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
