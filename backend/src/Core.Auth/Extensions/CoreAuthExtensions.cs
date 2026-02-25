using Core.Auth.Configuration;
using Core.Auth.Middleware;
using Core.Auth.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Auth.Extensions;

public static class CoreAuthExtensions
{
    public static IServiceCollection AddCoreAuth(
        this IServiceCollection services,
        Action<CoreAuthOptions>? configure = null)
    {
        var options = new CoreAuthOptions();
        configure?.Invoke(options);

        services.Configure<CoreAuthOptions>(opt =>
        {
            opt.SessionTimeout = options.SessionTimeout;
            opt.CookieName = options.CookieName;
            opt.RateLimitMaxAttempts = options.RateLimitMaxAttempts;
            opt.RateLimitLockoutMinutes = options.RateLimitLockoutMinutes;
            opt.BcryptWorkFactor = options.BcryptWorkFactor;
            opt.MinPasswordLength = options.MinPasswordLength;
            opt.MinFullNameLength = options.MinFullNameLength;
            opt.DefaultRoles = options.DefaultRoles;
        });

        services.AddDistributedMemoryCache();
        services.AddSession(sessionOptions =>
        {
            sessionOptions.IdleTimeout = options.SessionTimeout;
            sessionOptions.Cookie.HttpOnly = true;
            sessionOptions.Cookie.IsEssential = true;
            sessionOptions.Cookie.SameSite = SameSiteMode.Lax;
            sessionOptions.Cookie.Name = options.CookieName;
        });

        services.AddScoped<ICoreAuthService, CoreAuthService>();
        services.AddSingleton<IRateLimitService, RateLimitService>();

        return services;
    }

    public static IApplicationBuilder UseCoreAuth(this IApplicationBuilder app)
    {
        app.UseSession();
        app.UseMiddleware<AuditMiddleware>();
        return app;
    }

    /// <summary>
    /// Seeds default roles on startup if configured and no roles exist yet.
    /// Call after EnsureCreated / Migrate.
    /// </summary>
    public static async Task SeedCoreAuthAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<ICoreAuthService>();
        var options = scope.ServiceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<CoreAuthOptions>>().Value;

        if (options.DefaultRoles.Count > 0)
            await authService.SeedDefaultRolesAsync(options.DefaultRoles);
    }
}
