using Core.Common.Entities;
using Core.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Core.Infrastructure.Extensions;

public static class SlugExtensions
{
    /// <summary>
    /// Auto-generates slugs for all tracked ISlugEntity entries that are being added or modified.
    /// Call this in your DbContext's SaveChanges/SaveChangesAsync override.
    /// </summary>
    public static void ApplySlugs(this ChangeTracker changeTracker)
    {
        foreach (var entry in changeTracker.Entries<ISlugEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                if (!string.IsNullOrWhiteSpace(entry.Entity.Name))
                    entry.Entity.Slug = entry.Entity.Name.ToSlug();
            }
        }
    }
}
