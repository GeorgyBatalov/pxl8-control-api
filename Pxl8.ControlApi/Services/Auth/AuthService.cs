using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pxl8.ControlApi.Data;
using Pxl8.ControlApi.Data.Entities;
using BCryptNet = BCrypt.Net.BCrypt;

namespace Pxl8.ControlApi.Services.Auth;

/// <summary>
/// JWT authentication service implementation
/// </summary>
public class AuthService : IAuthService
{
    private readonly ControlDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly TimeSpan _jwtExpiry = TimeSpan.FromHours(24);

    public AuthService(
        ControlDbContext dbContext,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;

        _jwtSecret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret configuration is required");
        _jwtIssuer = _configuration["Jwt:Issuer"] ?? "pxl8-control-api";
        _jwtAudience = _configuration["Jwt:Audience"] ?? "pxl8-control-api";

        if (_jwtSecret.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Secret must be at least 32 characters");
        }
    }

    /// <inheritdoc />
    public async Task<(string Token, DateTimeOffset ExpiresAt)?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        // Find user by email
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Authentication failed: user not found. Email: {Email}", email);
            return null;
        }

        // Verify password
        if (!BCryptNet.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Authentication failed: invalid password. UserId: {UserId}", user.Id);
            return null;
        }

        // Generate JWT token
        var expiresAt = DateTimeOffset.UtcNow.Add(_jwtExpiry);
        var token = GenerateJwtToken(user, expiresAt);

        _logger.LogInformation("Authentication successful. UserId: {UserId}, Role: {Role}", user.Id, user.Role);

        return (token, expiresAt);
    }

    /// <inheritdoc />
    public (Guid UserId, UserRole Role, Guid? TenantId)? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtIssuer,
                ValidAudience = _jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // Extract claims
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;
            var tenantIdClaim = principal.FindFirst("TenantId")?.Value;

            if (userIdClaim == null || roleClaim == null)
            {
                _logger.LogWarning("Token validation failed: missing required claims");
                return null;
            }

            var userId = Guid.Parse(userIdClaim);
            var role = Enum.Parse<UserRole>(roleClaim);
            var tenantId = tenantIdClaim != null ? Guid.Parse(tenantIdClaim) : (Guid?)null;

            return (userId, role, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        return BCryptNet.HashPassword(password);
    }

    /// <summary>
    /// Generate JWT token for user
    /// </summary>
    private string GenerateJwtToken(User user, DateTimeOffset expiresAt)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        if (user.TenantId.HasValue)
        {
            claims.Add(new Claim("TenantId", user.TenantId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
