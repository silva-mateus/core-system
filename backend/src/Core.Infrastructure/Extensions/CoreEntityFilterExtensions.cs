using Core.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Extensions;

public static class CoreEntityFilterExtensions
{
    /// <summary>
    /// Applies a global query filter that excludes soft-deleted entities.
    /// Call on each entity type in OnModelCreating: builder.Entity&lt;T&gt;().ApplySoftDeleteFilter()
    /// </summary>
    public static EntityTypeBuilder<T> ApplySoftDeleteFilter<T>(this EntityTypeBuilder<T> builder)
        where T : class, ISoftDeletable
    {
        builder.HasQueryFilter(e => !e.IsDeleted);
        return builder;
    }

    /// <summary>
    /// Applies a global query filter that restricts entities to the current workspace.
    /// Requires a workspaceId parameter that should be resolved from the current request context.
    /// Call on each entity type in OnModelCreating.
    /// </summary>
    public static EntityTypeBuilder<T> ApplyWorkspaceFilter<T>(
        this EntityTypeBuilder<T> builder,
        int workspaceId)
        where T : class, IWorkspaceOwnedEntity
    {
        builder.HasQueryFilter(e => e.WorkspaceId == workspaceId);
        return builder;
    }
}
