using Core.Auth.Helpers;
using Core.Auth.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Core.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public class CoreAuthController : ControllerBase
{
    private readonly ICoreAuthService _authService;
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<CoreAuthController> _logger;

    public CoreAuthController(
        ICoreAuthService authService,
        IRateLimitService rateLimitService,
        ILogger<CoreAuthController> logger)
    {
        _authService = authService;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Usuário e senha são obrigatórios." });

        var ip = CoreAuthHelper.GetClientIp(HttpContext);
        var rateLimitKey = _rateLimitService.BuildLoginKey(ip, request.Username);

        if (_rateLimitService.IsRateLimited(rateLimitKey))
        {
            _logger.LogWarning("Rate limited login attempt for {Username} from {IP}", request.Username, ip);
            return StatusCode(429, new { error = "Muitas tentativas. Tente novamente em alguns minutos." });
        }

        var result = await _authService.ValidateUserAsync(request.Username, request.Password);

        if (result.IsFailure)
        {
            _rateLimitService.RecordAttempt(rateLimitKey);
            _logger.LogWarning("Failed login for {Username} from {IP}: {Error}", request.Username, ip, result.Error);
            return Unauthorized(new { error = result.Error, code = result.ErrorCode });
        }

        var user = result.Value!;
        _rateLimitService.ResetAttempts(rateLimitKey);

        CoreAuthHelper.SetSession(HttpContext, user.Id, user.RoleId, user.Role?.Name ?? "", user.Username);

        var permissions = user.Role?.Permissions.Select(p => p.PermissionKey).ToList() ?? [];

        return Ok(new
        {
            user = new
            {
                user.Id,
                user.Username,
                user.FullName,
                role = user.Role?.Name,
                user.MustChangePassword
            },
            permissions
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        CoreAuthHelper.ClearSession(HttpContext);
        return Ok(new { message = "Logout realizado com sucesso." });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = CoreAuthHelper.GetCurrentUserId(HttpContext);
        if (!userId.HasValue)
            return Unauthorized(new { error = "Não autenticado." });

        var user = await _authService.GetUserWithRoleAsync(userId.Value);
        if (user is null)
        {
            CoreAuthHelper.ClearSession(HttpContext);
            return Unauthorized(new { error = "Usuário não encontrado." });
        }

        if (!user.IsActive)
        {
            CoreAuthHelper.ClearSession(HttpContext);
            return Unauthorized(new { error = "Conta desativada." });
        }

        var permissions = user.Role?.Permissions.Select(p => p.PermissionKey).ToList() ?? [];

        return Ok(new
        {
            user = new
            {
                user.Id,
                user.Username,
                user.FullName,
                role = user.Role?.Name,
                user.MustChangePassword
            },
            permissions
        });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var authCheck = CoreAuthHelper.CheckAuthentication(HttpContext);
        if (authCheck is not null)
            return Unauthorized(new { error = "Não autenticado." });

        var userId = CoreAuthHelper.GetCurrentUserId(HttpContext)!.Value;

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { error = "Senha atual e nova senha são obrigatórias." });

        var result = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(new { message = "Senha alterada com sucesso." });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var authCheck = CoreAuthHelper.CheckAuthentication(HttpContext);
        if (authCheck is not null)
            return Unauthorized(new { error = "Não autenticado." });

        var userId = CoreAuthHelper.GetCurrentUserId(HttpContext)!.Value;

        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length < 2)
            return BadRequest(new { error = "Nome deve ter pelo menos 2 caracteres." });

        var user = await _authService.GetUserByIdAsync(userId);
        if (user is null)
            return NotFound(new { error = "Usuário não encontrado." });

        var result = await _authService.UpdateUserAsync(userId, request.FullName, user.RoleId);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(new { message = "Perfil atualizado com sucesso." });
    }
}

public record LoginRequest(string Username, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record UpdateProfileRequest(string FullName);
