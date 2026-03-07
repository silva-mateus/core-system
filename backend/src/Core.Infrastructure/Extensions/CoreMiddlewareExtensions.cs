using Core.Infrastructure.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Core.Infrastructure.Extensions;

public static class CoreMiddlewareExtensions
{
    /// <summary>
    /// Adds the global exception handler middleware that maps exceptions to RFC 7807 ProblemDetails.
    /// Built-in mappings are provided for Core exceptions (NotFoundException, ForbiddenException, etc.).
    /// Use the configure action to register custom exception mappings for app-specific exceptions.
    /// </summary>
    public static IApplicationBuilder UseCoreExceptionHandler(
        this IApplicationBuilder app,
        Action<CoreExceptionHandlerOptions>? configure = null)
    {
        var options = new CoreExceptionHandlerOptions();
        configure?.Invoke(options);
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>(options);
        return app;
    }

    /// <summary>
    /// Adds standard security headers to all HTTP responses.
    /// Includes X-Content-Type-Options, X-Frame-Options, CSP, and more.
    /// </summary>
    public static IApplicationBuilder UseCoreSecurityHeaders(this IApplicationBuilder app)
    {
        app.UseMiddleware<SecurityHeadersMiddleware>();
        return app;
    }
}
