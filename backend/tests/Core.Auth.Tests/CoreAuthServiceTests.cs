using Core.Auth.Configuration;
using Core.Auth.Models;
using Core.Auth.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Core.Auth.Tests;

public class CoreAuthServiceTests : IDisposable
{
    private readonly TestDbContext _db;
    private readonly CoreAuthService _service;

    public CoreAuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new TestDbContext(options);

        var authOptions = Options.Create(new CoreAuthOptions
        {
            BcryptWorkFactor = 4,
            MinPasswordLength = 4,
            RateLimitMaxAttempts = 5,
            RateLimitLockoutMinutes = 15
        });

        _service = new CoreAuthService(_db, authOptions, Mock.Of<ILogger<CoreAuthService>>());
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private async Task<CoreRole> SeedRoleAsync(string name = "admin", List<string>? permissions = null)
    {
        var role = new CoreRole
        {
            Name = name,
            Description = name,
            Permissions = (permissions ?? new List<string> { "manage_music", "manage_lists" })
                .Select(p => new RolePermission { PermissionKey = p }).ToList()
        };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return role;
    }

    private async Task<CoreUser> SeedUserAsync(CoreRole role, string username = "testuser", string password = "password123")
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password, 4);
        var user = new CoreUser
        {
            Username = username,
            FullName = "Test User",
            PasswordHash = hash,
            RoleId = role.Id,
            IsActive = true,
            MustChangePassword = false
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    #region ValidateUserAsync

    [Fact]
    public async Task ValidateUserAsync_ValidCredentials_ShouldSucceed()
    {
        var role = await SeedRoleAsync();
        await SeedUserAsync(role);

        var result = await _service.ValidateUserAsync("testuser", "password123");

        Assert.True(result.IsSuccess);
        Assert.Equal("testuser", result.Value!.Username);
    }

    [Fact]
    public async Task ValidateUserAsync_InvalidPassword_ShouldFail()
    {
        var role = await SeedRoleAsync();
        await SeedUserAsync(role);

        var result = await _service.ValidateUserAsync("testuser", "wrongpassword");

        Assert.True(result.IsFailure);
        Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateUserAsync_NonexistentUser_ShouldFail()
    {
        var result = await _service.ValidateUserAsync("nobody", "password");

        Assert.True(result.IsFailure);
        Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateUserAsync_DeactivatedUser_ShouldFail()
    {
        var role = await SeedRoleAsync();
        var user = await SeedUserAsync(role);
        user.IsActive = false;
        await _db.SaveChangesAsync();

        var result = await _service.ValidateUserAsync("testuser", "password123");

        Assert.True(result.IsFailure);
        Assert.Equal("ACCOUNT_DISABLED", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateUserAsync_ShouldUpdateLastLoginDate()
    {
        var role = await SeedRoleAsync();
        var user = await SeedUserAsync(role);

        await _service.ValidateUserAsync("testuser", "password123");

        var updated = await _db.Users.FindAsync(user.Id);
        Assert.NotNull(updated!.LastLoginDate);
    }

    #endregion

    #region CreateUserAsync

    [Fact]
    public async Task CreateUserAsync_ValidData_ShouldCreateUser()
    {
        var role = await SeedRoleAsync();

        var result = await _service.CreateUserAsync("newuser", "New User", "password123", role.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("newuser", result.Value!.Username);
        Assert.True(result.Value.MustChangePassword);
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateUsername_ShouldFail()
    {
        var role = await SeedRoleAsync();
        await SeedUserAsync(role);

        var result = await _service.CreateUserAsync("testuser", "Another User", "pass", role.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("USERNAME_TAKEN", result.ErrorCode);
    }

    [Fact]
    public async Task CreateUserAsync_InvalidRole_ShouldFail()
    {
        var result = await _service.CreateUserAsync("user", "User", "pass", 999);

        Assert.True(result.IsFailure);
        Assert.Equal("ROLE_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldNormalizeUsername()
    {
        var role = await SeedRoleAsync();

        var result = await _service.CreateUserAsync("  UserName  ", "Test", "pass", role.Id);

        Assert.Equal("username", result.Value!.Username);
    }

    #endregion

    #region ChangePasswordAsync

    [Fact]
    public async Task ChangePasswordAsync_ValidCurrentPassword_ShouldSucceed()
    {
        var role = await SeedRoleAsync();
        var user = await SeedUserAsync(role);

        var result = await _service.ChangePasswordAsync(user.Id, "password123", "newpass123");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ShouldFail()
    {
        var role = await SeedRoleAsync();
        var user = await SeedUserAsync(role);

        var result = await _service.ChangePasswordAsync(user.Id, "wrongpassword", "newpass");

        Assert.True(result.IsFailure);
        Assert.Equal("WRONG_PASSWORD", result.ErrorCode);
    }

    [Fact]
    public async Task ChangePasswordAsync_TooShortNewPassword_ShouldFail()
    {
        var role = await SeedRoleAsync();
        var user = await SeedUserAsync(role);

        var result = await _service.ChangePasswordAsync(user.Id, "password123", "ab");

        Assert.True(result.IsFailure);
        Assert.Equal("PASSWORD_TOO_SHORT", result.ErrorCode);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldClearMustChangeFlag()
    {
        var role = await SeedRoleAsync();
        var user = await SeedUserAsync(role);
        user.MustChangePassword = true;
        await _db.SaveChangesAsync();

        await _service.ChangePasswordAsync(user.Id, "password123", "newpass123");

        var updated = await _db.Users.FindAsync(user.Id);
        Assert.False(updated!.MustChangePassword);
    }

    #endregion

    #region ResetPasswordAsync

    [Fact]
    public async Task ResetPasswordAsync_ShouldSetMustChangeFlag()
    {
        var role = await SeedRoleAsync();
        var user = await SeedUserAsync(role);

        var result = await _service.ResetPasswordAsync(user.Id, "resetted123");

        Assert.True(result.IsSuccess);
        var updated = await _db.Users.FindAsync(user.Id);
        Assert.True(updated!.MustChangePassword);
    }

    [Fact]
    public async Task ResetPasswordAsync_NonexistentUser_ShouldFail()
    {
        var result = await _service.ResetPasswordAsync(999, "pass");

        Assert.True(result.IsFailure);
        Assert.Equal("USER_NOT_FOUND", result.ErrorCode);
    }

    #endregion

    #region User Management

    [Fact]
    public async Task DeactivateUserAsync_ShouldDeactivate()
    {
        var role = await SeedRoleAsync();
        var user = await SeedUserAsync(role);

        var result = await _service.DeactivateUserAsync(user.Id);

        Assert.True(result.IsSuccess);
        var updated = await _db.Users.FindAsync(user.Id);
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task ActivateUserAsync_ShouldActivate()
    {
        var role = await SeedRoleAsync();
        var user = await SeedUserAsync(role);
        user.IsActive = false;
        await _db.SaveChangesAsync();

        var result = await _service.ActivateUserAsync(user.Id);

        Assert.True(result.IsSuccess);
        var updated = await _db.Users.FindAsync(user.Id);
        Assert.True(updated!.IsActive);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldRemoveUser()
    {
        var role = await SeedRoleAsync();
        var user = await SeedUserAsync(role);

        var result = await _service.DeleteUserAsync(user.Id);

        Assert.True(result.IsSuccess);
        Assert.Null(await _db.Users.FindAsync(user.Id));
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateFields()
    {
        var role = await SeedRoleAsync();
        var user = await SeedUserAsync(role);

        var result = await _service.UpdateUserAsync(user.Id, "New Name", role.Id);

        Assert.True(result.IsSuccess);
        var updated = await _db.Users.FindAsync(user.Id);
        Assert.Equal("New Name", updated!.FullName);
    }

    #endregion

    #region Role Management

    [Fact]
    public async Task CreateRoleAsync_ShouldCreateWithPermissions()
    {
        var result = await _service.CreateRoleAsync("editor", "Editor role", new List<string> { "edit_music", "view_music" });

        Assert.True(result.IsSuccess);
        Assert.Equal("editor", result.Value!.Name);
        Assert.Equal(2, result.Value.Permissions.Count);
    }

    [Fact]
    public async Task CreateRoleAsync_DuplicateName_ShouldFail()
    {
        await SeedRoleAsync("admin");

        var result = await _service.CreateRoleAsync("admin", null, new List<string>());

        Assert.True(result.IsFailure);
        Assert.Equal("ROLE_EXISTS", result.ErrorCode);
    }

    [Fact]
    public async Task DeleteRoleAsync_WithUsers_ShouldFail()
    {
        var role = await SeedRoleAsync();
        await SeedUserAsync(role);

        var result = await _service.DeleteRoleAsync(role.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("ROLE_HAS_USERS", result.ErrorCode);
    }

    [Fact]
    public async Task DeleteRoleAsync_WithoutUsers_ShouldSucceed()
    {
        var role = await SeedRoleAsync();

        var result = await _service.DeleteRoleAsync(role.Id);

        Assert.True(result.IsSuccess);
        Assert.Null(await _db.Roles.FindAsync(role.Id));
    }

    #endregion

    #region Permissions

    [Fact]
    public async Task UserHasPermissionAsync_WithPermission_ShouldReturnTrue()
    {
        var role = await SeedRoleAsync("admin", new List<string> { "manage_music" });
        var user = await SeedUserAsync(role);

        Assert.True(await _service.UserHasPermissionAsync(user.Id, "manage_music"));
    }

    [Fact]
    public async Task UserHasPermissionAsync_WithoutPermission_ShouldReturnFalse()
    {
        var role = await SeedRoleAsync("viewer", new List<string> { "view_music" });
        var user = await SeedUserAsync(role);

        Assert.False(await _service.UserHasPermissionAsync(user.Id, "manage_music"));
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ShouldReturnAllPermissions()
    {
        var perms = new List<string> { "a", "b", "c" };
        var role = await SeedRoleAsync("multi", perms);
        var user = await SeedUserAsync(role);

        var result = await _service.GetUserPermissionsAsync(user.Id);

        Assert.Equal(3, result.Count);
        Assert.Contains("a", result);
        Assert.Contains("b", result);
        Assert.Contains("c", result);
    }

    #endregion

    #region Seeding

    [Fact]
    public async Task SeedDefaultRolesAsync_ShouldCreateRoles()
    {
        var defaults = new Dictionary<string, List<string>>
        {
            ["admin"] = new() { "manage_all" },
            ["viewer"] = new() { "view_music" }
        };

        await _service.SeedDefaultRolesAsync(defaults);

        var roles = await _db.Roles.Include(r => r.Permissions).ToListAsync();
        Assert.Equal(2, roles.Count);
        Assert.True(roles.First().IsDefault);
    }

    [Fact]
    public async Task SeedDefaultRolesAsync_WhenRolesExist_ShouldSkip()
    {
        await SeedRoleAsync();

        await _service.SeedDefaultRolesAsync(new Dictionary<string, List<string>>
        {
            ["new_role"] = new() { "perm" }
        });

        var roles = await _db.Roles.ToListAsync();
        Assert.Single(roles);
    }

    #endregion
}
