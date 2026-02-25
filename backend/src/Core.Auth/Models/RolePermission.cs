namespace Core.Auth.Models;

public class RolePermission
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public CoreRole? Role { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
}
