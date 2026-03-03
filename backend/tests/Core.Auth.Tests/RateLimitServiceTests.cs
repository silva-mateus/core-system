using Core.Auth.Configuration;
using Core.Auth.Services;
using Microsoft.Extensions.Options;

namespace Core.Auth.Tests;

public class RateLimitServiceTests : IDisposable
{
    private readonly RateLimitService _service;

    public RateLimitServiceTests()
    {
        var options = Options.Create(new CoreAuthOptions
        {
            RateLimitMaxAttempts = 3,
            RateLimitLockoutMinutes = 15
        });
        _service = new RateLimitService(options);
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    [Fact]
    public void IsRateLimited_NoAttempts_ShouldReturnFalse()
    {
        Assert.False(_service.IsRateLimited("key1"));
    }

    [Fact]
    public void IsRateLimited_UnderThreshold_ShouldReturnFalse()
    {
        _service.RecordAttempt("key2");
        _service.RecordAttempt("key2");

        Assert.False(_service.IsRateLimited("key2"));
    }

    [Fact]
    public void IsRateLimited_AtThreshold_ShouldReturnTrue()
    {
        for (int i = 0; i < 3; i++)
            _service.RecordAttempt("key3");

        Assert.True(_service.IsRateLimited("key3"));
    }

    [Fact]
    public void IsRateLimited_OverThreshold_ShouldReturnTrue()
    {
        for (int i = 0; i < 5; i++)
            _service.RecordAttempt("key4");

        Assert.True(_service.IsRateLimited("key4"));
    }

    [Fact]
    public void ResetAttempts_ShouldClearRateLimit()
    {
        for (int i = 0; i < 3; i++)
            _service.RecordAttempt("key5");

        Assert.True(_service.IsRateLimited("key5"));

        _service.ResetAttempts("key5");

        Assert.False(_service.IsRateLimited("key5"));
    }

    [Fact]
    public void BuildLoginKey_ShouldFormatCorrectly()
    {
        var key = _service.BuildLoginKey("192.168.1.1", "UserName");

        Assert.Equal("login:192.168.1.1:username", key);
    }

    [Fact]
    public void BuildLoginKey_ShouldTrimAndLowercase()
    {
        var key = _service.BuildLoginKey("10.0.0.1", "  Admin  ");

        Assert.Equal("login:10.0.0.1:admin", key);
    }

    [Fact]
    public void DifferentKeys_ShouldBeIndependent()
    {
        for (int i = 0; i < 3; i++)
            _service.RecordAttempt("user1");

        Assert.True(_service.IsRateLimited("user1"));
        Assert.False(_service.IsRateLimited("user2"));
    }
}
