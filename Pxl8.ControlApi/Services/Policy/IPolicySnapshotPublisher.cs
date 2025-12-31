using Pxl8.ControlApi.Contracts.V1.PolicySnapshot;

namespace Pxl8.ControlApi.Services.Policy;

/// <summary>
/// Policy snapshot publisher service - generate atomic policy snapshots
/// </summary>
public interface IPolicySnapshotPublisher
{
    /// <summary>
    /// Generate current policy snapshot for all tenants
    /// </summary>
    /// <remarks>
    /// Returns atomic snapshot of all tenant policies (domains, quotas, settings)
    /// Data Plane polls this endpoint every 1 minute
    /// </remarks>
    Task<PolicySnapshotDto> GenerateSnapshotAsync(CancellationToken cancellationToken = default);
}
