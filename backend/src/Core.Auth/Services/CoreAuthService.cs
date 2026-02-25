using Core.Auth.Configuration;
using Core.Auth.Models;
using Core.Common.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Auth.Services;

public class CoreAuthService : ICoreAuthService
{
    private readonly DbContext _db;
    private readonly CoreAuthOptions _options;
    private readonly ILogger<CoreAuthService> _logger;

    public CoreAuthService(DbContext db, IOptions<CoreAuthOptions> options, ILogger<CoreAuthService> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    private DbSet<CoreUser> Users => _db.Set<CoreUser>();
    private DbSet<CoreRole> Roles => _db.Set<CoreRole>();
    private DbSet<RolePermission> RolePermissions => _db.Set<RolePermission>();

    public async Task<Result<CoreUser>> ValidateUserAsync(string username, string password)
    {
        var user = await Users
            .Include(u => u.Role)
            .ThenInclude(r => r!.Permissions)
            .FirstOrDefaultAsync(u => u.Username == username.ToLower().Trim());

        if (user is null)
            return Result.Failure<CoreUser>("Usuário ou senha inválidos.", "INVALID_CREDENTIALS");

        if (!user.IsActive)
            return Result.Failure<CoreUser>("Conta desativada.", "ACCOUNT_DISABLED");

        if (!VerifyPassword(password, user.PasswordHash))
            return Result.Failure<CoreUser>("Usuário ou senha inválidos.", "INVALID_CREDENTIALS");

        user.LastLoginDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {Username} logged in successfully", user.Username);
        return Result.Success(user);
    }

    public async Task<CoreUser?> GetUserByIdAsync(int id)
        => await Users.FindAsync(id);

    public async Task<CoreUser?> GetUserWithRoleAsync(int id)
        => await Users
            .Include(u => u.Role)
            .ThenInclude(r => r!.Permissions)
            .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<List<CoreUser>> GetAllUsersAsync()
        => await Users.Include(u => u.Role).OrderBy(u => u.Username).ToListAsync();

    public async Task<Result<CoreUser>> CreateUserAsync(string username, string fullName, string password, int roleId)
    {
        var normalized = username.ToLower().Trim();

        if (await Users.AnyAsync(u => u.Username == normalized))
            return Result.Failure<CoreUser>("Nome de usuário já existe.", "USERNAME_TAKEN");

        var role = await Roles.FindAsync(roleId);
        if (role is null)
            return Result.Failure<CoreUser>("Role não encontrada.", "ROLE_NOT_FOUND");

        var user = new CoreUser
        {
            Username = normalized,
            FullName = fullName.Trim(),
            PasswordHash = HashPassword(password),
            RoleId = roleId,
            MustChangePassword = true
        };

        Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {Username} created with role {Role}", user.Username, role.Name);
        return Result.Success(user);
    }

    public async Task<Result> UpdateUserAsync(int id, string fullName, int roleId)
    {
        var user = await Users.FindAsync(id);
        if (user is null)
            return Result.Failure("Usuário não encontrado.", "USER_NOT_FOUND");

        var role = await Roles.FindAsync(roleId);
        if (role is null)
            return Result.Failure("Role não encontrada.", "ROLE_NOT_FOUND");

        user.FullName = fullName.Trim();
        user.RoleId = roleId;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeactivateUserAsync(int id)
    {
        var user = await Users.FindAsync(id);
        if (user is null)
            return Result.Failure("Usuário não encontrado.", "USER_NOT_FOUND");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ActivateUserAsync(int id)
    {
        var user = await Users.FindAsync(id);
        if (user is null)
            return Result.Failure("Usuário não encontrado.", "USER_NOT_FOUND");

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteUserAsync(int id)
    {
        var user = await Users.FindAsync(id);
        if (user is null)
            return Result.Failure("Usuário não encontrado.", "USER_NOT_FOUND");

        Users.Remove(user);
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await Users.FindAsync(userId);
        if (user is null)
            return Result.Failure("Usuário não encontrado.", "USER_NOT_FOUND");

        if (!VerifyPassword(currentPassword, user.PasswordHash))
            return Result.Failure("Senha atual incorreta.", "WRONG_PASSWORD");

        if (newPassword.Length < _options.MinPasswordLength)
            return Result.Failure($"A senha deve ter pelo menos {_options.MinPasswordLength} caracteres.", "PASSWORD_TOO_SHORT");

        user.PasswordHash = HashPassword(newPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(int userId, string newPassword)
    {
        var user = await Users.FindAsync(userId);
        if (user is null)
            return Result.Failure("Usuário não encontrado.", "USER_NOT_FOUND");

        user.PasswordHash = HashPassword(newPassword);
        user.MustChangePassword = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    // Roles

    public async Task<CoreRole?> GetRoleByIdAsync(int id)
        => await Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id);

    public async Task<List<CoreRole>> GetAllRolesAsync()
        => await Roles.Include(r => r.Permissions).OrderBy(r => r.Name).ToListAsync();

    public async Task<Result<CoreRole>> CreateRoleAsync(string name, string? description, List<string> permissionKeys)
    {
        var normalized = name.ToLower().Trim();
        if (await Roles.AnyAsync(r => r.Name == normalized))
            return Result.Failure<CoreRole>("Role já existe.", "ROLE_EXISTS");

        var role = new CoreRole
        {
            Name = normalized,
            Description = description?.Trim(),
            Permissions = permissionKeys.Select(k => new RolePermission { PermissionKey = k }).ToList()
        };

        Roles.Add(role);
        await _db.SaveChangesAsync();
        return Result.Success(role);
    }

    public async Task<Result> UpdateRoleAsync(int id, string name, string? description, List<string> permissionKeys)
    {
        var role = await Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id);
        if (role is null)
            return Result.Failure("Role não encontrada.", "ROLE_NOT_FOUND");

        role.Name = name.ToLower().Trim();
        role.Description = description?.Trim();
        role.UpdatedAt = DateTime.UtcNow;

        // Replace all permissions
        RolePermissions.RemoveRange(role.Permissions);
        role.Permissions = permissionKeys.Select(k => new RolePermission { RoleId = id, PermissionKey = k }).ToList();

        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteRoleAsync(int id)
    {
        var role = await Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id);
        if (role is null)
            return Result.Failure("Role não encontrada.", "ROLE_NOT_FOUND");

        if (await Users.AnyAsync(u => u.RoleId == id))
            return Result.Failure("Não é possível deletar uma role com usuários associados.", "ROLE_HAS_USERS");

        Roles.Remove(role);
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> SetDefaultRoleAsync(int id)
    {
        var role = await Roles.FindAsync(id);
        if (role is null)
            return Result.Failure("Role não encontrada.", "ROLE_NOT_FOUND");

        await Roles.ExecuteUpdateAsync(r => r.SetProperty(x => x.IsDefault, false));
        role.IsDefault = true;
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    // Permissions

    public async Task<List<string>> GetUserPermissionsAsync(int userId)
    {
        var user = await GetUserWithRoleAsync(userId);
        return user?.Role?.Permissions.Select(p => p.PermissionKey).ToList() ?? [];
    }

    public async Task<bool> UserHasPermissionAsync(int userId, string permissionKey)
    {
        var permissions = await GetUserPermissionsAsync(userId);
        return permissions.Contains(permissionKey);
    }

    // Seeding

    public async Task SeedDefaultRolesAsync(Dictionary<string, List<string>> defaultRoles)
    {
        if (await Roles.AnyAsync()) return;

        var isFirst = true;
        foreach (var (name, permissions) in defaultRoles)
        {
            var role = new CoreRole
            {
                Name = name.ToLower(),
                Description = name,
                IsDefault = isFirst,
                Permissions = permissions.Select(p => new RolePermission { PermissionKey = p }).ToList()
            };
            Roles.Add(role);
            isFirst = false;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} default roles", defaultRoles.Count);
    }

    // Password hashing

    private string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, _options.BcryptWorkFactor);

    private static bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
