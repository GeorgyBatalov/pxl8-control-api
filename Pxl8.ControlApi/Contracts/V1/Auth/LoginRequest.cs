using System.Text.Json.Serialization;

namespace Pxl8.ControlApi.Contracts.V1.Auth;

/// <summary>
/// Login request DTO
/// </summary>
public record LoginRequest
{
    /// <summary>
    /// User email
    /// </summary>
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>
    /// User password (plain text)
    /// </summary>
    [JsonPropertyName("password")]
    public required string Password { get; init; }
}
