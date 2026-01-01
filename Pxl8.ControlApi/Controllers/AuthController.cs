using Microsoft.AspNetCore.Mvc;
using Pxl8.ControlApi.Contracts.V1.Auth;
using Pxl8.ControlApi.Contracts.V1.Common;
using Pxl8.ControlApi.Services.Auth;

namespace Pxl8.ControlApi.Controllers;

/// <summary>
/// Authentication API - JWT login
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/auth/login
    /// </summary>
    /// <remarks>
    /// Authenticate user and return JWT token
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "INVALID_REQUEST",
                Message = "Email and password are required",
                TraceId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        // Authenticate user
        var result = await _authService.AuthenticateAsync(request.Email, request.Password, cancellationToken);

        if (result == null)
        {
            return Unauthorized(new ErrorResponse
            {
                ErrorCode = "INVALID_CREDENTIALS",
                Message = "Invalid email or password",
                TraceId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        var (token, expiresAt) = result.Value;

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt
        });
    }
}
