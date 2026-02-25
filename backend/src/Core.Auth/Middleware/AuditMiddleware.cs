using Core.Auth.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Core.Auth.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    private static readonly HashSet<string> MutatingMethods =
        ["POST", "PUT", "DELETE", "PATCH"];

    private static readonly string[] SkipPaths =
        ["/api/health", "/api/monitoring", "/swagger", "/_next", "/favicon"];

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldAudit(context))
        {
            await _next(context);
            return;
        }

        var userId = CoreAuthHelper.GetCurrentUserId(context);
        var username = CoreAuthHelper.GetCurrentUsername(context) ?? "anonymous";
        var ip = CoreAuthHelper.GetClientIp(context);
        var method = context.Request.Method;
        var path = context.Request.Path.Value;

        await _next(context);

        var statusCode = context.Response.StatusCode;
        if (statusCode is >= 200 and < 300)
        {
            _logger.LogInformation(
                "AUDIT: {Method} {Path} by {Username} (ID:{UserId}) from {IP} -> {StatusCode}",
                method, path, username, userId, ip, statusCode);
        }
    }

    private static bool ShouldAudit(HttpContext context)
    {
        if (!MutatingMethods.Contains(context.Request.Method))
            return false;

        var path = context.Request.Path.Value ?? "";
        return !SkipPaths.Any(skip => path.StartsWith(skip, StringComparison.OrdinalIgnoreCase));
    }
}
