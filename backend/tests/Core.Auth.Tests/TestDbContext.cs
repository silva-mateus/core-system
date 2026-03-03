using Core.Auth.Models;
using Microsoft.EntityFrameworkCore;

namespace Core.Auth.Tests;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<CoreUser> Users => Set<CoreUser>();
    public DbSet<CoreRole> Roles => Set<CoreRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CoreUser>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.HasOne(u => u.Role).WithMany().HasForeignKey(u => u.RoleId);
        });

        modelBuilder.Entity<CoreRole>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasOne(p => p.Role).WithMany(r => r.Permissions).HasForeignKey(p => p.RoleId);
        });
    }
}
