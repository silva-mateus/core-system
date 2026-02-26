using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Events;

public class SseConnectionManager : ISseService
{
    private readonly ConcurrentDictionary<string, SseClient> _clients = new();
    private readonly ILogger<SseConnectionManager> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public SseConnectionManager(ILogger<SseConnectionManager> logger)
    {
        _logger = logger;
    }

    public int ConnectedClients => _clients.Count;

    public async Task AddClientAsync(string clientId, HttpResponse response, CancellationToken cancellationToken, IEnumerable<SseEvent>? initialEvents = null)
    {
        response.Headers.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers.Connection = "keep-alive";

        var client = new SseClient(clientId, response);
        _clients.TryAdd(clientId, client);

        _logger.LogInformation("SSE client {ClientId} connected. Total: {Count}", clientId, _clients.Count);

        try
        {
            await WriteEventAsync(response, "connected", new { client_id = clientId }, cancellationToken);

            if (initialEvents != null)
            {
                foreach (var evt in initialEvents)
                    await WriteEventAsync(response, evt.EventName, evt.Data, cancellationToken);
            }

            await response.Body.FlushAsync(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(30_000, cancellationToken);
                await WriteCommentAsync(response, "keepalive", cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SSE client {ClientId} disconnected with error", clientId);
        }
        finally
        {
            RemoveClient(clientId);
        }
    }

    public void RemoveClient(string clientId)
    {
        if (_clients.TryRemove(clientId, out _))
            _logger.LogInformation("SSE client {ClientId} removed. Total: {Count}", clientId, _clients.Count);
    }

    public async Task BroadcastAsync(string eventName, object data, CancellationToken cancellationToken = default)
    {
        var disconnected = new List<string>();

        foreach (var (id, client) in _clients)
        {
            try
            {
                await WriteEventAsync(client.Response, eventName, data, cancellationToken);
                await client.Response.Body.FlushAsync(cancellationToken);
            }
            catch
            {
                disconnected.Add(id);
            }
        }

        foreach (var id in disconnected)
            RemoveClient(id);
    }

    public async Task SendToClientAsync(string clientId, string eventName, object data, CancellationToken cancellationToken = default)
    {
        if (!_clients.TryGetValue(clientId, out var client))
            return;

        try
        {
            await WriteEventAsync(client.Response, eventName, data, cancellationToken);
            await client.Response.Body.FlushAsync(cancellationToken);
        }
        catch
        {
            RemoveClient(clientId);
        }
    }

    private static async Task WriteEventAsync(HttpResponse response, string eventName, object data, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await response.WriteAsync($"event: {eventName}\n", ct);
        await response.WriteAsync($"data: {json}\n\n", ct);
    }

    private static async Task WriteCommentAsync(HttpResponse response, string comment, CancellationToken ct)
    {
        await response.WriteAsync($": {comment}\n\n", ct);
        await response.Body.FlushAsync(ct);
    }

    private sealed record SseClient(string Id, HttpResponse Response);
}
