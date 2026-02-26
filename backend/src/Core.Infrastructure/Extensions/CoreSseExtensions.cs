using Core.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Infrastructure.Extensions;

public static class CoreSseExtensions
{
    /// <summary>
    /// Registers the SSE connection manager as a singleton.
    /// </summary>
    public static IServiceCollection AddCoreSse(this IServiceCollection services)
    {
        services.AddSingleton<ISseService, SseConnectionManager>();
        return services;
    }
}
