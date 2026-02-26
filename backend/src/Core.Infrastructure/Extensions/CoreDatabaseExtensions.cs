using Core.Auth.Models;
using Core.FileManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Infrastructure.Extensions;

public static class CoreDatabaseExtensions
{
    /// <summary>
    /// Registers the DbContext with MySQL (Pomelo) using standardized configuration.
    /// </summary>
    public static IServiceCollection AddCoreDatabase<TContext>(
        this IServiceCollection services,
        string connectionString) where TContext : DbContext
    {
        services.AddDbContext<TContext>(options =>
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mysql =>
            {
                mysql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
                mysql.CommandTimeout(30);
                mysql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        // Register base DbContext so Core services can resolve it
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());

        return services;
    }

    /// <summary>
    /// Applies Core.Auth entity configurations (Users, Roles, RolePermissions).
    /// Call this inside your DbContext's OnModelCreating.
    /// </summary>
    public static ModelBuilder ApplyCoreAuthEntities(this ModelBuilder builder)
    {
        builder.Entity<CoreUser>(entity =>
        {
            entity.ToTable("core_users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(200).IsRequired();

            entity.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CoreRole>(entity =>
        {
            entity.ToTable("core_roles");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("core_role_permissions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoleId, e.PermissionKey }).IsUnique();
            entity.Property(e => e.PermissionKey).HasMaxLength(100).IsRequired();

            entity.HasOne(e => e.Role)
                .WithMany(r => r.Permissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        return builder;
    }

    /// <summary>
    /// Applies Core.FileManagement entity configurations (StoredFiles).
    /// Call this inside your DbContext's OnModelCreating.
    /// </summary>
    public static ModelBuilder ApplyCoreFileEntities(this ModelBuilder builder)
    {
        builder.Entity<StoredFile>(entity =>
        {
            entity.ToTable("core_stored_files");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FileHash);
            entity.HasIndex(e => e.Category);
            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.OriginalName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.RelativePath).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(200).IsRequired();
            entity.Property(e => e.FileHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(200);
        });

        return builder;
    }
}
