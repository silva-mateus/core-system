using Core.FileManagement.Configuration;
using Core.FileManagement.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Core.FileManagement.Tests;

public class CoreFileServiceTests : IDisposable
{
    private readonly TestFileDbContext _db;
    private readonly CoreFileService _service;
    private readonly string _storagePath;

    public CoreFileServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<TestFileDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new TestFileDbContext(dbOptions);

        _storagePath = Path.Combine(Path.GetTempPath(), $"core_file_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_storagePath);

        var fileOptions = Options.Create(new CoreFileOptions
        {
            StoragePath = _storagePath,
            MaxFileSizeBytes = 10 * 1024 * 1024,
            AllowedExtensions = new[] { ".pdf", ".txt" },
            OrganizeByCategory = true,
            DeduplicateByHash = true
        });

        _service = new CoreFileService(_db, fileOptions, Mock.Of<ILogger<CoreFileService>>());
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
        if (Directory.Exists(_storagePath))
        {
            try { Directory.Delete(_storagePath, true); } catch { }
        }
    }

    private static IFormFile CreateFormFile(string filename, string content = "test content", string contentType = "application/pdf")
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", filename) { Headers = new HeaderDictionary(), ContentType = contentType };
    }

    #region SaveFileAsync

    [Fact]
    public async Task SaveFileAsync_ValidFile_ShouldSaveAndReturnStoredFile()
    {
        var file = CreateFormFile("test.pdf");

        var result = await _service.SaveFileAsync(file, "Documents", 1);

        Assert.True(result.IsSuccess);
        Assert.Equal("test.pdf", result.Value!.FileName);
        Assert.Equal(1, result.Value.UploadedByUserId);
        Assert.NotEmpty(result.Value.FileHash);
    }

    [Fact]
    public async Task SaveFileAsync_EmptyFile_ShouldFail()
    {
        var file = CreateFormFile("empty.pdf", "");

        var result = await _service.SaveFileAsync(file);

        Assert.True(result.IsFailure);
        Assert.Equal("EMPTY_FILE", result.ErrorCode);
    }

    [Fact]
    public async Task SaveFileAsync_DisallowedExtension_ShouldFail()
    {
        var file = CreateFormFile("virus.exe");

        var result = await _service.SaveFileAsync(file);

        Assert.True(result.IsFailure);
        Assert.Equal("INVALID_EXTENSION", result.ErrorCode);
    }

    [Fact]
    public async Task SaveFileAsync_DuplicateHash_ShouldFail()
    {
        var file1 = CreateFormFile("first.pdf", "same content");
        var file2 = CreateFormFile("second.pdf", "same content");

        await _service.SaveFileAsync(file1);
        var result = await _service.SaveFileAsync(file2);

        Assert.True(result.IsFailure);
        Assert.Equal("DUPLICATE_FILE", result.ErrorCode);
    }

    [Fact]
    public async Task SaveFileAsync_WithCategory_ShouldOrganizeInSubfolder()
    {
        var file = CreateFormFile("doc.pdf");

        var result = await _service.SaveFileAsync(file, "Reports");

        Assert.True(result.IsSuccess);
        Assert.Contains("Reports", result.Value!.RelativePath);
    }

    #endregion

    #region DeleteFileAsync

    [Fact]
    public async Task DeleteFileAsync_ExistingFile_ShouldDeleteDbAndPhysical()
    {
        var file = CreateFormFile("to_delete.txt", "delete me");
        var saved = await _service.SaveFileAsync(file);
        var fullPath = _service.GetAbsolutePath(saved.Value!.RelativePath);
        Assert.True(File.Exists(fullPath));

        var result = await _service.DeleteFileAsync(saved.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.False(File.Exists(fullPath));
        Assert.Null(await _db.StoredFiles.FindAsync(saved.Value.Id));
    }

    [Fact]
    public async Task DeleteFileAsync_NonexistentFile_ShouldFail()
    {
        var result = await _service.DeleteFileAsync(999);

        Assert.True(result.IsFailure);
        Assert.Equal("FILE_NOT_FOUND", result.ErrorCode);
    }

    #endregion

    #region ComputeFileHash

    [Fact]
    public void ComputeFileHash_SameContent_ShouldReturnSameHash()
    {
        var content = "identical"u8.ToArray();
        using var s1 = new MemoryStream(content);
        using var s2 = new MemoryStream(content);

        Assert.Equal(_service.ComputeFileHash(s1), _service.ComputeFileHash(s2));
    }

    [Fact]
    public void ComputeFileHash_DifferentContent_ShouldReturnDifferentHash()
    {
        using var s1 = new MemoryStream("content A"u8.ToArray());
        using var s2 = new MemoryStream("content B"u8.ToArray());

        Assert.NotEqual(_service.ComputeFileHash(s1), _service.ComputeFileHash(s2));
    }

    [Fact]
    public void ComputeFileHash_ShouldReturnLowercaseHex()
    {
        using var s = new MemoryStream("test"u8.ToArray());
        var hash = _service.ComputeFileHash(s);

        Assert.Matches("^[a-f0-9]+$", hash);
    }

    #endregion

    #region GetAbsolutePath

    [Fact]
    public void GetAbsolutePath_ShouldCombineWithStoragePath()
    {
        var result = _service.GetAbsolutePath("subfolder/file.pdf");
        Assert.Contains(_storagePath, result);
        Assert.EndsWith("file.pdf", result);
    }

    #endregion

    #region GetFileByIdAsync / GetAllFilesAsync

    [Fact]
    public async Task GetFileByIdAsync_ExistingFile_ShouldReturn()
    {
        var file = CreateFormFile("lookup.txt", "find me");
        var saved = await _service.SaveFileAsync(file);

        var found = await _service.GetFileByIdAsync(saved.Value!.Id);

        Assert.NotNull(found);
        Assert.Equal("lookup.txt", found!.FileName);
    }

    [Fact]
    public async Task GetAllFilesAsync_ShouldReturnAllFiles()
    {
        await _service.SaveFileAsync(CreateFormFile("a.pdf", "a"));
        await _service.SaveFileAsync(CreateFormFile("b.txt", "b"));

        var all = await _service.GetAllFilesAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetFilesByCategoryAsync_ShouldFilterByCategory()
    {
        await _service.SaveFileAsync(CreateFormFile("cat1.pdf", "c1"), "Cat1");
        await _service.SaveFileAsync(CreateFormFile("cat2.txt", "c2"), "Cat2");

        var result = await _service.GetFilesByCategoryAsync("Cat1");

        Assert.Single(result);
    }

    #endregion
}
