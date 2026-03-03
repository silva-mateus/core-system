using Core.Auth.Helpers;
using Core.Auth.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Moq;

namespace Core.Auth.Tests;

public class CoreAuthHelperTests
{
    private static HttpContext CreateHttpContext(Dictionary<string, byte[]>? sessionData = null)
    {
        var ctx = new DefaultHttpContext();
        var session = new TestSession(sessionData ?? new Dictionary<string, byte[]>());
        ctx.Features.Set<ISessionFeature>(new TestSessionFeature { Session = session });
        ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        return ctx;
    }

    #region IsAuthenticated / CheckAuthentication

    [Fact]
    public void IsAuthenticated_WithUserId_ShouldReturnTrue()
    {
        var ctx = CreateHttpContext(new Dictionary<string, byte[]>
        {
            ["UserId"] = BitConverter.GetBytes(1)
        });

        Assert.True(CoreAuthHelper.IsAuthenticated(ctx));
    }

    [Fact]
    public void IsAuthenticated_WithoutUserId_ShouldReturnFalse()
    {
        var ctx = CreateHttpContext();

        Assert.False(CoreAuthHelper.IsAuthenticated(ctx));
    }

    [Fact]
    public void CheckAuthentication_Authenticated_ShouldReturnNull()
    {
        var ctx = CreateHttpContext(new Dictionary<string, byte[]>
        {
            ["UserId"] = BitConverter.GetBytes(1)
        });

        Assert.Null(CoreAuthHelper.CheckAuthentication(ctx));
    }

    [Fact]
    public void CheckAuthentication_NotAuthenticated_ShouldReturnUnauthorized()
    {
        var ctx = CreateHttpContext();

        var result = CoreAuthHelper.CheckAuthentication(ctx);

        Assert.NotNull(result);
    }

    #endregion

    #region Session Management

    [Fact]
    public void SetSession_ShouldSetAllValues()
    {
        var ctx = CreateHttpContext();

        CoreAuthHelper.SetSession(ctx, 42, 2, "admin", "johndoe");

        Assert.Equal(42, CoreAuthHelper.GetCurrentUserId(ctx));
        Assert.Equal(2, CoreAuthHelper.GetCurrentRoleId(ctx));
        Assert.Equal("admin", CoreAuthHelper.GetCurrentRoleName(ctx));
        Assert.Equal("johndoe", CoreAuthHelper.GetCurrentUsername(ctx));
    }

    [Fact]
    public void ClearSession_ShouldClearAllValues()
    {
        var ctx = CreateHttpContext(new Dictionary<string, byte[]>
        {
            ["UserId"] = BitConverter.GetBytes(1),
            ["Username"] = System.Text.Encoding.UTF8.GetBytes("user")
        });

        CoreAuthHelper.ClearSession(ctx);

        Assert.False(CoreAuthHelper.IsAuthenticated(ctx));
        Assert.Null(CoreAuthHelper.GetCurrentUsername(ctx));
    }

    #endregion

    #region GetClientIp

    [Fact]
    public void GetClientIp_WithXForwardedFor_ShouldReturnFirstIp()
    {
        var ctx = CreateHttpContext();
        ctx.Request.Headers["X-Forwarded-For"] = "203.0.113.1, 70.41.3.18";

        Assert.Equal("203.0.113.1", CoreAuthHelper.GetClientIp(ctx));
    }

    [Fact]
    public void GetClientIp_WithoutForwardedHeader_ShouldReturnRemoteIp()
    {
        var ctx = CreateHttpContext();

        Assert.Equal("127.0.0.1", CoreAuthHelper.GetClientIp(ctx));
    }

    [Fact]
    public void GetClientIp_NoIpAtAll_ShouldReturnUnknown()
    {
        var ctx = new DefaultHttpContext();
        var session = new TestSession(new Dictionary<string, byte[]>());
        ctx.Features.Set<ISessionFeature>(new TestSessionFeature { Session = session });

        Assert.Equal("unknown", CoreAuthHelper.GetClientIp(ctx));
    }

    #endregion

    #region HasPermissionAsync

    [Fact]
    public async Task HasPermissionAsync_Authenticated_ShouldCheckPermission()
    {
        var ctx = CreateHttpContext(new Dictionary<string, byte[]>
        {
            ["UserId"] = BitConverter.GetBytes(5)
        });

        var authMock = new Mock<ICoreAuthService>();
        authMock.Setup(a => a.UserHasPermissionAsync(It.IsAny<int>(), "manage_music")).ReturnsAsync(true);

        Assert.True(await CoreAuthHelper.HasPermissionAsync(ctx, authMock.Object, "manage_music"));
    }

    [Fact]
    public async Task HasPermissionAsync_NotAuthenticated_ShouldReturnFalse()
    {
        var ctx = CreateHttpContext();
        var authMock = new Mock<ICoreAuthService>();

        Assert.False(await CoreAuthHelper.HasPermissionAsync(ctx, authMock.Object, "manage_music"));
    }

    #endregion
}

internal sealed class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _data;

    public TestSession(Dictionary<string, byte[]> data) => _data = data;

    public bool IsAvailable => true;
    public string Id => "test-session-id";
    public IEnumerable<string> Keys => _data.Keys;

    public void Clear() => _data.Clear();
    public void Remove(string key) => _data.Remove(key);
    public void Set(string key, byte[] value) => _data[key] = value;

    public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out byte[]? value) =>
        _data.TryGetValue(key, out value);

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class TestSessionFeature : ISessionFeature
{
    public ISession Session { get; set; } = null!;
}
