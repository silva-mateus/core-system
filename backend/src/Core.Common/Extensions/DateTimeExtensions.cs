namespace Core.Common.Extensions;

public static class DateTimeExtensions
{
    public static string ToRelativeTime(this DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var diff = now - dateTime;

        return diff.TotalSeconds switch
        {
            < 60 => "agora",
            < 3600 => $"{(int)diff.TotalMinutes}min atrás",
            < 86400 => $"{(int)diff.TotalHours}h atrás",
            < 2592000 => $"{(int)diff.TotalDays}d atrás",
            _ => dateTime.ToString("dd/MM/yyyy")
        };
    }

    public static string ToShortDate(this DateTime dateTime)
        => dateTime.ToString("dd/MM/yyyy");

    public static string ToShortDateTime(this DateTime dateTime)
        => dateTime.ToString("dd/MM/yyyy HH:mm");
}
