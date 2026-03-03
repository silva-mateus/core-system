using Core.Infrastructure.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Core.Infrastructure.Tests;

public class SseConnectionManagerTests
{
    private readonly SseConnectionManager _manager;

    public SseConnectionManagerTests()
    {
        _manager = new SseConnectionManager(Mock.Of<ILogger<SseConnectionManager>>());
    }

    [Fact]
    public void ConnectedClients_Initially_ShouldBeZero()
    {
        Assert.Equal(0, _manager.ConnectedClients);
    }

    [Fact]
    public void RemoveClient_NonexistentClient_ShouldNotThrow()
    {
        var ex = Record.Exception(() => _manager.RemoveClient("nonexistent"));
        Assert.Null(ex);
    }

    [Fact]
    public async Task BroadcastAsync_NoClients_ShouldNotThrow()
    {
        var ex = await Record.ExceptionAsync(() =>
            _manager.BroadcastAsync("test", new { message = "hello" }));

        Assert.Null(ex);
    }

    [Fact]
    public async Task SendToClientAsync_NonexistentClient_ShouldNotThrow()
    {
        var ex = await Record.ExceptionAsync(() =>
            _manager.SendToClientAsync("fake", "event", new { }));

        Assert.Null(ex);
    }
}
