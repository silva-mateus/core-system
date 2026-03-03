using Core.FileManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace Core.FileManagement.Tests;

public class TestFileDbContext : DbContext
{
    public TestFileDbContext(DbContextOptions<TestFileDbContext> options) : base(options) { }

    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredFile>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => f.FileHash);
        });
    }
}
