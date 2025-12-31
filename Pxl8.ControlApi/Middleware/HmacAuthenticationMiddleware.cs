using Pxl8.ControlApi.Security;
using System.Text;

namespace Pxl8.ControlApi.Middleware;

/// <summary>
/// HMAC authentication middleware for Control API
/// Validates X-Signature and X-Timestamp headers on all inter-plane API calls
/// </summary>
public class HmacAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HmacAuthenticationMiddleware> _logger;
    private readonly bool _enabled;

    // Endpoints that require HMAC authentication (inter-plane communication)
    private static readonly HashSet<string> ProtectedEndpoints = new()
    {
        "/api/policy-snapshot",
        "/api/budget-allocation",
        "/api/usage-report"
    };

    public HmacAuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<HmacAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _enabled = configuration.GetValue<bool>("InterPlane:HmacEnabled", true);
    }

    public async Task InvokeAsync(HttpContext context, InterPlaneHmacService hmacService)
    {
        // Skip HMAC validation if disabled (for local dev)
        if (!_enabled)
        {
            _logger.LogDebug("HMAC authentication disabled (local dev mode)");
            await _next(context);
            return;
        }

        // Skip HMAC for non-protected endpoints (health checks, public APIs)
        var path = context.Request.Path.Value ?? "";
        if (!IsProtectedEndpoint(path))
        {
            await _next(context);
            return;
        }

        // Extract HMAC headers
        if (!context.Request.Headers.TryGetValue("X-Signature", out var signature) ||
            !context.Request.Headers.TryGetValue("X-Timestamp", out var timestampStr))
        {
            _logger.LogWarning(
                "HMAC authentication failed: Missing X-Signature or X-Timestamp header. Path: {Path}",
                path);

            context.Response.StatusCode = 401; // Unauthorized
            await context.Response.WriteAsJsonAsync(new
            {
                Error = "Unauthorized",
                Message = "Missing HMAC authentication headers"
            });
            return;
        }

        // Parse timestamp
        if (!long.TryParse(timestampStr, out var timestamp))
        {
            _logger.LogWarning("HMAC authentication failed: Invalid timestamp format. Path: {Path}", path);

            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                Error = "Unauthorized",
                Message = "Invalid timestamp format"
            });
            return;
        }

        // Read request body for signature validation
        context.Request.EnableBuffering(); // Allow multiple reads
        string body;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0; // Reset stream for next middleware
        }

        // Validate HMAC signature
        var isValid = hmacService.ValidateSignature(
            context.Request.Method,
            path,
            body,
            timestamp,
            signature!);

        if (!isValid)
        {
            _logger.LogWarning(
                "HMAC authentication failed: Invalid signature. Method: {Method}, Path: {Path}",
                context.Request.Method, path);

            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                Error = "Unauthorized",
                Message = "Invalid HMAC signature"
            });
            return;
        }

        // Signature valid - proceed to next middleware
        _logger.LogDebug("HMAC authentication successful. Path: {Path}", path);
        await _next(context);
    }

    private static bool IsProtectedEndpoint(string path)
    {
        return ProtectedEndpoints.Any(endpoint => path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));
    }
}
