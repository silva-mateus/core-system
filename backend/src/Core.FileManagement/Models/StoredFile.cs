using Core.Common.Entities;

namespace Core.FileManagement.Models;

public class StoredFile : BaseEntity, IAuditableEntity
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? UploadedByUserId { get; set; }
    public string? Category { get; set; }

    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
}
