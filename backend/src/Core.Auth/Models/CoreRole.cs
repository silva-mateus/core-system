using Core.Common.Entities;

namespace Core.Auth.Models;

public class CoreRole : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public ICollection<RolePermission> Permissions { get; set; } = [];
}
