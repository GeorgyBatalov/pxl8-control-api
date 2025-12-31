using Pxl8.ControlApi.Contracts.V1.BudgetAllocation;

namespace Pxl8.ControlApi.Services.Budget;

/// <summary>
/// Budget allocator service - allocate TTL-based budget leases to Data Planes
/// </summary>
public interface IBudgetAllocatorService
{
    /// <summary>
    /// Allocate budget lease for Data Plane
    /// </summary>
    /// <remarks>
    /// Idempotent by request_id
    /// Enforces ONE active lease per (tenant, period, dataplane)
    /// </remarks>
    Task<BudgetAllocateResponse> AllocateBudgetAsync(BudgetAllocateRequest request, CancellationToken cancellationToken = default);
}
