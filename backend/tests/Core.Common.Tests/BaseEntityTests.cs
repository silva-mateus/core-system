using Core.Common.Entities;

namespace Core.Common.Tests;

public class BaseEntityTests
{
    private class TestEntity : BaseEntity { }

    [Fact]
    public void CreatedAt_ShouldDefaultToUtcNow()
    {
        var before = DateTime.UtcNow;
        var entity = new TestEntity();
        var after = DateTime.UtcNow;

        Assert.InRange(entity.CreatedAt, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public void UpdatedAt_ShouldDefaultToNull()
    {
        var entity = new TestEntity();
        Assert.Null(entity.UpdatedAt);
    }

    [Fact]
    public void Id_ShouldDefaultToZero()
    {
        var entity = new TestEntity();
        Assert.Equal(0, entity.Id);
    }
}
