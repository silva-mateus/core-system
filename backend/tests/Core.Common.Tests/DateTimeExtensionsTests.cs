using Core.Common.Extensions;

namespace Core.Common.Tests;

public class DateTimeExtensionsTests
{
    [Fact]
    public void ToRelativeTime_JustNow_ShouldReturnAgora()
    {
        var result = DateTime.UtcNow.AddSeconds(-5).ToRelativeTime();
        Assert.Equal("agora", result);
    }

    [Fact]
    public void ToRelativeTime_Minutes_ShouldReturnMinFormat()
    {
        var result = DateTime.UtcNow.AddMinutes(-15).ToRelativeTime();
        Assert.Equal("15min atrás", result);
    }

    [Fact]
    public void ToRelativeTime_Hours_ShouldReturnHourFormat()
    {
        var result = DateTime.UtcNow.AddHours(-3).ToRelativeTime();
        Assert.Equal("3h atrás", result);
    }

    [Fact]
    public void ToRelativeTime_Days_ShouldReturnDayFormat()
    {
        var result = DateTime.UtcNow.AddDays(-5).ToRelativeTime();
        Assert.Equal("5d atrás", result);
    }

    [Fact]
    public void ToRelativeTime_Months_ShouldReturnDateFormat()
    {
        var dt = DateTime.UtcNow.AddDays(-60);
        var result = dt.ToRelativeTime();
        Assert.Equal(dt.ToString("dd/MM/yyyy"), result);
    }

    [Fact]
    public void ToShortDate_ShouldFormatCorrectly()
    {
        var dt = new DateTime(2026, 3, 15);
        Assert.Equal("15/03/2026", dt.ToShortDate());
    }

    [Fact]
    public void ToShortDateTime_ShouldFormatCorrectly()
    {
        var dt = new DateTime(2026, 3, 15, 14, 30, 0);
        Assert.Equal("15/03/2026 14:30", dt.ToShortDateTime());
    }

    [Fact]
    public void ToRelativeTime_BoundaryAt60Seconds_ShouldReturnMinutes()
    {
        var result = DateTime.UtcNow.AddSeconds(-61).ToRelativeTime();
        Assert.Equal("1min atrás", result);
    }

    [Fact]
    public void ToRelativeTime_BoundaryAt60Minutes_ShouldReturnHours()
    {
        var result = DateTime.UtcNow.AddMinutes(-61).ToRelativeTime();
        Assert.Equal("1h atrás", result);
    }
}
