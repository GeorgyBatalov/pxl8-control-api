using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pxl8.ControlApi.Data;

namespace Pxl8.ControlApi.Controllers;

/// <summary>
/// Health Check API - monitoring and diagnostics
/// </summary>
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ControlDbContext _db;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ControlDbContext db, ILogger<HealthController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// GET /health/live
    /// </summary>
    /// <remarks>
    /// Liveness probe - is the service running?
    /// </remarks>
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetLiveness()
    {
        return Ok(new
        {
            status = "healthy",
            service = "pxl8-control-api",
            timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// GET /health/ready
    /// </summary>
    /// <remarks>
    /// Readiness probe - is the service ready to serve traffic?
    /// Checks: Database connectivity
    /// </remarks>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReadiness(CancellationToken cancellationToken)
    {
        var checks = new Dictionary<string, object>();
        var isHealthy = true;

        // Check: Database connectivity
        try
        {
            // Simple query to check DB connection
            await _db.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);

            checks["database"] = new
            {
                status = "healthy",
                provider = "PostgreSQL"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");

            checks["database"] = new
            {
                status = "unhealthy",
                error = ex.Message
            };

            isHealthy = false;
        }

        if (!isHealthy)
        {
            return StatusCode(503, new
            {
                status = "unhealthy",
                service = "pxl8-control-api",
                checks,
                timestamp = DateTimeOffset.UtcNow
            });
        }

        return Ok(new
        {
            status = "healthy",
            service = "pxl8-control-api",
            checks,
            timestamp = DateTimeOffset.UtcNow
        });
    }
}
