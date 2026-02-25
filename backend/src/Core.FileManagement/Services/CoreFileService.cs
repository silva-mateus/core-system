using System.Security.Cryptography;
using Core.Common.Results;
using Core.FileManagement.Configuration;
using Core.FileManagement.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.FileManagement.Services;

public class CoreFileService : ICoreFileService
{
    private readonly DbContext _db;
    private readonly CoreFileOptions _options;
    private readonly ILogger<CoreFileService> _logger;

    public CoreFileService(DbContext db, IOptions<CoreFileOptions> options, ILogger<CoreFileService> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    private DbSet<StoredFile> Files => _db.Set<StoredFile>();

    public async Task<Result<StoredFile>> SaveFileAsync(IFormFile file, string? category = null, int? uploadedByUserId = null)
    {
        if (file.Length == 0)
            return Result.Failure<StoredFile>("Arquivo vazio.", "EMPTY_FILE");

        if (file.Length > _options.MaxFileSizeBytes)
            return Result.Failure<StoredFile>($"Arquivo excede o tamanho máximo de {_options.MaxFileSizeBytes / (1024 * 1024)}MB.", "FILE_TOO_LARGE");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (_options.AllowedExtensions.Length > 0 && !_options.AllowedExtensions.Contains(extension))
            return Result.Failure<StoredFile>($"Extensão '{extension}' não permitida.", "INVALID_EXTENSION");

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var hash = ComputeFileHash(memoryStream);

        if (_options.DeduplicateByHash)
        {
            var existing = await Files.FirstOrDefaultAsync(f => f.FileHash == hash);
            if (existing is not null)
                return Result.Failure<StoredFile>("Arquivo duplicado já existe.", "DUPLICATE_FILE");
        }

        var sanitizedName = SanitizeFileName(file.FileName);
        var folder = BuildFolderPath(category);
        Directory.CreateDirectory(folder);

        var uniqueName = GetUniqueFileName(folder, sanitizedName);
        var fullPath = Path.Combine(folder, uniqueName);

        memoryStream.Position = 0;
        await using (var fileStream = new FileStream(fullPath, FileMode.Create))
        {
            await memoryStream.CopyToAsync(fileStream);
        }

        var relativePath = Path.GetRelativePath(_options.StoragePath, fullPath).Replace('\\', '/');

        var storedFile = new StoredFile
        {
            FileName = uniqueName,
            OriginalName = file.FileName,
            RelativePath = relativePath,
            ContentType = file.ContentType ?? "application/octet-stream",
            FileHash = hash,
            FileSize = file.Length,
            UploadedByUserId = uploadedByUserId,
            Category = category,
            CreatedByUserId = uploadedByUserId
        };

        Files.Add(storedFile);
        await _db.SaveChangesAsync();

        _logger.LogInformation("File saved: {FileName} ({Size} bytes) by user {UserId}",
            uniqueName, file.Length, uploadedByUserId);

        return Result.Success(storedFile);
    }

    public async Task<Result> DeleteFileAsync(int fileId)
    {
        var file = await Files.FindAsync(fileId);
        if (file is null)
            return Result.Failure("Arquivo não encontrado.", "FILE_NOT_FOUND");

        var fullPath = GetAbsolutePath(file.RelativePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        Files.Remove(file);
        await _db.SaveChangesAsync();

        _logger.LogInformation("File deleted: {FileName}", file.FileName);
        return Result.Success();
    }

    public async Task<StoredFile?> GetFileByIdAsync(int fileId)
        => await Files.FindAsync(fileId);

    public async Task<List<StoredFile>> GetAllFilesAsync()
        => await Files.OrderByDescending(f => f.CreatedAt).ToListAsync();

    public async Task<List<StoredFile>> GetFilesByCategoryAsync(string category)
        => await Files.Where(f => f.Category == category).OrderByDescending(f => f.CreatedAt).ToListAsync();

    public string GetAbsolutePath(string relativePath)
        => Path.GetFullPath(Path.Combine(_options.StoragePath, relativePath));

    public string ComputeFileHash(Stream stream)
    {
        var hash = MD5.HashData(stream);
        stream.Position = 0;
        return Convert.ToHexStringLower(hash);
    }

    private string BuildFolderPath(string? category)
    {
        if (_options.OrganizeByCategory && !string.IsNullOrWhiteSpace(category))
            return Path.Combine(_options.StoragePath, SanitizeFileName(category));
        return _options.StoragePath;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalid.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }

    private static string GetUniqueFileName(string folder, string fileName)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var candidate = fileName;
        var counter = 1;

        while (File.Exists(Path.Combine(folder, candidate)))
        {
            candidate = $"{nameWithoutExt} ({counter}){extension}";
            counter++;
        }

        return candidate;
    }
}
