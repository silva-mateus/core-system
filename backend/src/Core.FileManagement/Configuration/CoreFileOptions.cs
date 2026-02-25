namespace Core.FileManagement.Configuration;

public class CoreFileOptions
{
    public string StoragePath { get; set; } = "./uploads";
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024; // 50 MB
    public string[] AllowedExtensions { get; set; } = [".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp"];
    public bool OrganizeByCategory { get; set; } = true;
    public bool DeduplicateByHash { get; set; } = true;
}
