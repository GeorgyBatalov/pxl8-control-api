using Microsoft.AspNetCore.Mvc;
using Pxl8.ControlApi.Contracts.V1.Common;
using Pxl8.ControlApi.Contracts.V1.UsageReporting;
using Pxl8.ControlApi.Services.Usage;

namespace Pxl8.ControlApi.Controllers;

/// <summary>
/// Usage Reporting API - receives usage reports from Data Planes
/// </summary>
[ApiController]
[Route("internal/v1/usage")]
public class UsageReportController : ControllerBase
{
    private readonly IUsageReportProcessor _processor;
    private readonly ILogger<UsageReportController> _logger;

    public UsageReportController(IUsageReportProcessor processor, ILogger<UsageReportController> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    /// <summary>
    /// POST /internal/v1/usage/report
    /// </summary>
    /// <remarks>
    /// Called by Data Planes every 10 seconds with usage deltas
    /// Idempotent by report_id - duplicate reports are ignored
    /// </remarks>
    [HttpPost("report")]
    [ProducesResponseType(typeof(UsageReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitUsageReport(
        [FromBody] UsageReportRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ReportId == Guid.Empty)
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "INVALID_REPORT_ID",
                Message = "report_id must be a valid GUID",
                TraceId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

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

        _logger.LogDebug(
            "Usage report received: report_id={ReportId}, dataplane={DataplaneId}, tenant={TenantId}",
            request.ReportId, request.DataplaneId, request.TenantId);

        var response = await _processor.ProcessReportAsync(request, cancellationToken);

        return Ok(response);
    }
}
