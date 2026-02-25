using Core.Auth.Models;
using Core.Common.Results;

namespace Core.Auth.Services;

public interface ICoreAuthService
{
    // Login
    Task<Result<CoreUser>> ValidateUserAsync(string username, string password);

    // User CRUD
    Task<CoreUser?> GetUserByIdAsync(int id);
    Task<CoreUser?> GetUserWithRoleAsync(int id);
    Task<List<CoreUser>> GetAllUsersAsync();
    Task<Result<CoreUser>> CreateUserAsync(string username, string fullName, string password, int roleId);
    Task<Result> UpdateUserAsync(int id, string fullName, int roleId);
    Task<Result> DeactivateUserAsync(int id);
    Task<Result> ActivateUserAsync(int id);
    Task<Result> DeleteUserAsync(int id);

    // Password
    Task<Result> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<Result> ResetPasswordAsync(int userId, string newPassword);

    // Roles
    Task<CoreRole?> GetRoleByIdAsync(int id);
    Task<List<CoreRole>> GetAllRolesAsync();
    Task<Result<CoreRole>> CreateRoleAsync(string name, string? description, List<string> permissionKeys);
    Task<Result> UpdateRoleAsync(int id, string name, string? description, List<string> permissionKeys);
    Task<Result> DeleteRoleAsync(int id);
    Task<Result> SetDefaultRoleAsync(int id);

    // Permissions
    Task<List<string>> GetUserPermissionsAsync(int userId);
    Task<bool> UserHasPermissionAsync(int userId, string permissionKey);

    // Seeding
    Task SeedDefaultRolesAsync(Dictionary<string, List<string>> defaultRoles);
}
