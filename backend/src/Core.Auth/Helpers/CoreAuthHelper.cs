using Core.Auth.Services;
using Microsoft.AspNetCore.Http;

namespace Core.Auth.Helpers;

public static class CoreAuthHelper
{
    public const string SessionUserId = "UserId";
    public const string SessionRoleId = "RoleId";
    public const string SessionRoleName = "RoleName";
    public const string SessionUsername = "Username";
    public const string SessionLastActivity = "LastActivity";

    public static int? GetCurrentUserId(HttpContext context)
        => context.Session.GetInt32(SessionUserId);

    public static int? GetCurrentRoleId(HttpContext context)
        => context.Session.GetInt32(SessionRoleId);

    public static string? GetCurrentUsername(HttpContext context)
        => context.Session.GetString(SessionUsername);

    public static string? GetCurrentRoleName(HttpContext context)
        => context.Session.GetString(SessionRoleName);

    public static bool IsAuthenticated(HttpContext context)
        => GetCurrentUserId(context).HasValue;

    public static IResult? CheckAuthentication(HttpContext context)
    {
        if (!IsAuthenticated(context))
            return Results.Unauthorized();
        return null;
    }

    public static async Task<bool> HasPermissionAsync(
        HttpContext context,
        ICoreAuthService authService,
        string permissionKey)
    {
        var userId = GetCurrentUserId(context);
        if (!userId.HasValue) return false;
        return await authService.UserHasPermissionAsync(userId.Value, permissionKey);
    }

    public static async Task<IResult?> CheckPermissionAsync(
        HttpContext context,
        ICoreAuthService authService,
        string permissionKey)
    {
        var authCheck = CheckAuthentication(context);
        if (authCheck is not null) return authCheck;

        if (!await HasPermissionAsync(context, authService, permissionKey))
            return Results.Forbid();

        return null;
    }

    public static void SetSession(HttpContext context, int userId, int roleId, string roleName, string username)
    {
        context.Session.SetInt32(SessionUserId, userId);
        context.Session.SetInt32(SessionRoleId, roleId);
        context.Session.SetString(SessionRoleName, roleName);
        context.Session.SetString(SessionUsername, username);
        context.Session.SetString(SessionLastActivity, DateTime.UtcNow.ToString("O"));
    }

    public static void ClearSession(HttpContext context)
    {
        context.Session.Clear();
    }

    public static string GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
            return forwarded.Split(',')[0].Trim();

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
