namespace Pxl8.ControlApi.Contracts.Abstractions;

/// <summary>
/// Marker interface for versioned contracts
/// </summary>
public interface IVersioned
{
    /// <summary>
    /// Contract version number (semantic versioning major version)
    /// </summary>
    int Version { get; }
}
