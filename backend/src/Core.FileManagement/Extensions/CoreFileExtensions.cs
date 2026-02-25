using Core.FileManagement.Configuration;
using Core.FileManagement.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Core.FileManagement.Extensions;

public static class CoreFileExtensions
{
    public static IServiceCollection AddCoreFileManagement(
        this IServiceCollection services,
        Action<CoreFileOptions>? configure = null)
    {
        var options = new CoreFileOptions();
        configure?.Invoke(options);

        services.Configure<CoreFileOptions>(opt =>
        {
            opt.StoragePath = options.StoragePath;
            opt.MaxFileSizeBytes = options.MaxFileSizeBytes;
            opt.AllowedExtensions = options.AllowedExtensions;
            opt.OrganizeByCategory = options.OrganizeByCategory;
            opt.DeduplicateByHash = options.DeduplicateByHash;
        });

        Directory.CreateDirectory(options.StoragePath);

        services.AddScoped<ICoreFileService, CoreFileService>();

        return services;
    }
}
