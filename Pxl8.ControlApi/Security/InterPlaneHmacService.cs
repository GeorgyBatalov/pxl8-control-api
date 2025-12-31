using System.Security.Cryptography;
using System.Text;

namespace Pxl8.ControlApi.Security;

/// <summary>
/// HMAC-SHA256 signature service for secure inter-plane API communication
/// Prevents unauthorized access to Control Plane endpoints from external sources
/// </summary>
public class InterPlaneHmacService
{
    private readonly string _sharedSecret;
    private readonly ILogger<InterPlaneHmacService> _logger;
    private readonly TimeSpan _maxTimestampAge = TimeSpan.FromMinutes(5); // Replay attack protection

    public InterPlaneHmacService(IConfiguration configuration, ILogger<InterPlaneHmacService> logger)
    {
        _sharedSecret = configuration["InterPlane:SharedSecret"]
            ?? throw new InvalidOperationException("InterPlane:SharedSecret configuration is missing");

        if (_sharedSecret.Length < 32)
        {
            throw new InvalidOperationException("InterPlane:SharedSecret must be at least 32 characters");
        }

        _logger = logger;
    }

    /// <summary>
    /// Generate HMAC signature for outgoing request (Data Gateway → Control API)
    /// </summary>
    /// <param name="httpMethod">HTTP method (GET, POST, PUT)</param>
    /// <param name="path">Request path (e.g., /api/policy-snapshot)</param>
    /// <param name="body">Request body (empty string if no body)</param>
    /// <param name="timestamp">Unix timestamp (seconds since epoch)</param>
    /// <returns>Base64-encoded HMAC-SHA256 signature</returns>
    public string GenerateSignature(string httpMethod, string path, string body, long timestamp)
    {
        // Message format: METHOD|PATH|BODY|TIMESTAMP
        // Example: GET|/api/policy-snapshot||1735689600
        var message = $"{httpMethod.ToUpperInvariant()}|{path}|{body}|{timestamp}";

        var keyBytes = Encoding.UTF8.GetBytes(_sharedSecret);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);

        // Return as base64 (no need for URL-safe encoding in headers)
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Validate HMAC signature for incoming request (Data Gateway → Control API)
    /// </summary>
    /// <param name="httpMethod">HTTP method</param>
    /// <param name="path">Request path</param>
    /// <param name="body">Request body</param>
    /// <param name="timestamp">Unix timestamp from X-Timestamp header</param>
    /// <param name="providedSignature">Signature from X-Signature header</param>
    /// <returns>True if signature is valid and timestamp is recent</returns>
    public bool ValidateSignature(
        string httpMethod,
        string path,
        string body,
        long timestamp,
        string providedSignature)
    {
        try
        {
            // Check timestamp freshness (replay attack protection)
            var requestTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            var age = DateTimeOffset.UtcNow - requestTime;

            if (Math.Abs(age.TotalMinutes) > _maxTimestampAge.TotalMinutes)
            {
                _logger.LogWarning(
                    "HMAC validation failed: Timestamp too old. Age: {Age}, Max: {MaxAge}",
                    age, _maxTimestampAge);
                return false;
            }

            // Generate expected signature
            var expectedSignature = GenerateSignature(httpMethod, path, body, timestamp);

            // Constant-time comparison (prevents timing attacks)
            var isValid = ConstantTimeCompare(providedSignature, expectedSignature);

            if (!isValid)
            {
                _logger.LogWarning(
                    "HMAC validation failed: Signature mismatch. Method: {Method}, Path: {Path}",
                    httpMethod, path);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating HMAC signature");
            return false;
        }
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks
    /// </summary>
    private bool ConstantTimeCompare(string a, string b)
    {
        if (a.Length != b.Length)
            return false;

        var result = 0;
        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }

    /// <summary>
    /// Get current Unix timestamp (seconds since epoch)
    /// </summary>
    public static long GetCurrentTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
