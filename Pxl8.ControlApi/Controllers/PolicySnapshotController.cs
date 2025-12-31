using Microsoft.AspNetCore.Mvc;
using Pxl8.ControlApi.Contracts.V1.PolicySnapshot;
using Pxl8.ControlApi.Services.Policy;

namespace Pxl8.ControlApi.Controllers;

/// <summary>
/// Policy Snapshot API - generates atomic policy snapshots for Data Planes
/// </summary>
[ApiController]
[Route("internal/v1/policy-snapshot")]
public class PolicySnapshotController : ControllerBase
{
    private readonly IPolicySnapshotPublisher _publisher;
    private readonly ILogger<PolicySnapshotController> _logger;

    public PolicySnapshotController(IPolicySnapshotPublisher publisher, ILogger<PolicySnapshotController> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// GET /internal/v1/policy-snapshot
    /// </summary>
    /// <remarks>
    /// Called by Data Planes every 1 minute to sync tenant policies
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PolicySnapshotDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPolicySnapshot(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Policy snapshot requested");

        var snapshot = await _publisher.GenerateSnapshotAsync(cancellationToken);

        return Ok(snapshot);
    }
}
