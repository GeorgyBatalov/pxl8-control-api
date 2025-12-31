using Microsoft.AspNetCore.Mvc;
using Pxl8.ControlApi.Contracts.V1.BudgetAllocation;
using Pxl8.ControlApi.Contracts.V1.Common;
using Pxl8.ControlApi.Services.Budget;

namespace Pxl8.ControlApi.Controllers;

/// <summary>
/// Budget Allocation API - issues TTL-based budget leases to Data Planes
/// </summary>
[ApiController]
[Route("internal/v1/budget")]
public class BudgetAllocationController : ControllerBase
{
    private readonly IBudgetAllocatorService _allocator;
    private readonly ILogger<BudgetAllocationController> _logger;

    public BudgetAllocationController(IBudgetAllocatorService allocator, ILogger<BudgetAllocationController> logger)
    {
        _allocator = allocator;
        _logger = logger;
    }

    /// <summary>
    /// POST /internal/v1/budget/allocate
    /// </summary>
    /// <remarks>
    /// Called by Data Planes when budget runs low (< 20% remaining)
    /// Idempotent by request_id - duplicate requests return same lease
    /// </remarks>
    [HttpPost("allocate")]
    [ProducesResponseType(typeof(BudgetAllocateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AllocateBudget(
        [FromBody] BudgetAllocateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty)
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "INVALID_TENANT_ID",
                Message = "tenant_id must be a valid GUID",
                TraceId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        if (request.PeriodId == Guid.Empty)
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "INVALID_PERIOD_ID",
                Message = "period_id must be a valid GUID",
                TraceId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        if (string.IsNullOrWhiteSpace(request.DataplaneId))
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "INVALID_DATAPLANE_ID",
                Message = "dataplane_id is required",
                TraceId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        _logger.LogInformation(
            "Budget allocation request: request_id={RequestId}, tenant={TenantId}, dataplane={DataplaneId}",
            request.RequestId, request.TenantId, request.DataplaneId);

        var response = await _allocator.AllocateBudgetAsync(request, cancellationToken);

        return Ok(response);
    }
}
