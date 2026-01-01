using System.Text.Json.Serialization;

namespace Pxl8.ControlApi.Contracts.V1.Auth;

/// <summary>
/// Login response DTO
/// </summary>
public record LoginResponse
{
    /// <summary>
    /// JWT token
    /// </summary>
    [JsonPropertyName("token")]
    public required string Token { get; init; }

    /// <summary>
    /// Token expiration timestamp (UTC)
    /// </summary>
    [JsonPropertyName("expires_at")]
    public required DateTimeOffset ExpiresAt { get; init; }
}
