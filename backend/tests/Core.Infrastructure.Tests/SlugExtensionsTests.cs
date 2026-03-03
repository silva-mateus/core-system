using Core.Common.Entities;
using Core.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Tests;

public class SlugExtensionsTests
{
    private class SlugEntity : ISlugEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }

    private class SlugTestContext : DbContext
    {
        public SlugTestContext(DbContextOptions<SlugTestContext> options) : base(options) { }
        public DbSet<SlugEntity> Entities => Set<SlugEntity>();
    }

    [Fact]
    public void ApplySlugs_OnAdd_ShouldGenerateSlug()
    {
        var options = new DbContextOptionsBuilder<SlugTestContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var ctx = new SlugTestContext(options);
        var entity = new SlugEntity { Name = "Hello World" };
        ctx.Entities.Add(entity);
        ctx.ChangeTracker.ApplySlugs();

        Assert.Equal("hello-world", entity.Slug);
    }

    [Fact]
    public void ApplySlugs_OnModify_ShouldUpdateSlug()
    {
        var options = new DbContextOptionsBuilder<SlugTestContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var ctx = new SlugTestContext(options);
        var entity = new SlugEntity { Name = "Original", Slug = "original" };
        ctx.Entities.Add(entity);
        ctx.SaveChanges();

        entity.Name = "Updated Name";
        ctx.Entry(entity).State = EntityState.Modified;
        ctx.ChangeTracker.ApplySlugs();

        Assert.Equal("updated-name", entity.Slug);
    }

    [Fact]
    public void ApplySlugs_WithAccents_ShouldNormalize()
    {
        var options = new DbContextOptionsBuilder<SlugTestContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var ctx = new SlugTestContext(options);
        var entity = new SlugEntity { Name = "Ação de Graças" };
        ctx.Entities.Add(entity);
        ctx.ChangeTracker.ApplySlugs();

        Assert.Equal("acao-de-gracas", entity.Slug);
    }

    [Fact]
    public void ApplySlugs_EmptyName_ShouldNotCrash()
    {
        var options = new DbContextOptionsBuilder<SlugTestContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var ctx = new SlugTestContext(options);
        var entity = new SlugEntity { Name = "" };
        ctx.Entities.Add(entity);

        var ex = Record.Exception(() => ctx.ChangeTracker.ApplySlugs());
        Assert.Null(ex);
    }
}
