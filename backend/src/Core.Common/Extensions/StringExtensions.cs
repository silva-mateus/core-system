using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Common.Extensions;

public static partial class StringExtensions
{
    public static string ToSlug(this string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var result = sb.ToString().Normalize(NormalizationForm.FormC);
        result = SlugRegex().Replace(result, "");
        result = WhitespaceRegex().Replace(result, "-");
        return result.Trim('-').ToLowerInvariant();
    }

    public static string ToTitleCase(this string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        var info = CultureInfo.CurrentCulture.TextInfo;
        return info.ToTitleCase(text.ToLower());
    }

    public static string Truncate(this string text, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return string.Concat(text.AsSpan(0, maxLength - suffix.Length), suffix);
    }

    public static string? NullIfEmpty(this string? text)
        => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    [GeneratedRegex(@"[^a-zA-Z0-9\s-]")]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
