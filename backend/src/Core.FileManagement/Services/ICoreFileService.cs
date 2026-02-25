using Core.Common.Results;
using Core.FileManagement.Models;
using Microsoft.AspNetCore.Http;

namespace Core.FileManagement.Services;

public interface ICoreFileService
{
    Task<Result<StoredFile>> SaveFileAsync(IFormFile file, string? category = null, int? uploadedByUserId = null);
    Task<Result> DeleteFileAsync(int fileId);
    Task<StoredFile?> GetFileByIdAsync(int fileId);
    Task<List<StoredFile>> GetAllFilesAsync();
    Task<List<StoredFile>> GetFilesByCategoryAsync(string category);
    string GetAbsolutePath(string relativePath);
    string ComputeFileHash(Stream stream);
}
