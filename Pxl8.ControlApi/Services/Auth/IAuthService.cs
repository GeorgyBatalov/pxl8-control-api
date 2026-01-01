using Pxl8.ControlApi.Data.Entities;

namespace Pxl8.ControlApi.Services.Auth;

/// <summary>
/// Authentication service interface
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticate user by email and password
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="password">Plain text password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT token and expiration if successful, null otherwise</returns>
    Task<(string Token, DateTimeOffset ExpiresAt)?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate JWT token and extract user claims
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID and role if valid, null otherwise</returns>
    (Guid UserId, UserRole Role, Guid? TenantId)? ValidateToken(string token);

    /// <summary>
    /// Hash password using BCrypt
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>BCrypt hash</returns>
    string HashPassword(string password);
}
