using Microsoft.AspNetCore.Http;

namespace Core.Infrastructure.Events;

public record SseEvent(string EventName, object Data);

public interface ISseService
{
    Task AddClientAsync(string clientId, HttpResponse response, CancellationToken cancellationToken, IEnumerable<SseEvent>? initialEvents = null);
    void RemoveClient(string clientId);
    Task BroadcastAsync(string eventName, object data, CancellationToken cancellationToken = default);
    Task SendToClientAsync(string clientId, string eventName, object data, CancellationToken cancellationToken = default);
    int ConnectedClients { get; }
}
