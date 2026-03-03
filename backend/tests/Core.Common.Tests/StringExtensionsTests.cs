using Core.Common.Extensions;

namespace Core.Common.Tests;

public class StringExtensionsTests
{
    #region ToSlug Tests

    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("Música Católica", "musica-catolica")]
    [InlineData("São Paulo", "sao-paulo")]
    [InlineData("Ação de Graças", "acao-de-gracas")]
    [InlineData("  Multiple   Spaces  ", "multiple-spaces")]
    [InlineData("Special!@#$%Characters", "specialcharacters")]
    [InlineData("Already-Slug", "already-slug")]
    public void ToSlug_ShouldGenerateCorrectSlug(string input, string expected)
    {
        Assert.Equal(expected, input.ToSlug());
    }

    [Theory]
    [InlineData("über cool", "uber-cool")]
    [InlineData("naïve approach", "naive-approach")]
    [InlineData("Ñoño", "nono")]
    public void ToSlug_ShouldHandleUnicodeCharacters(string input, string expected)
    {
        Assert.Equal(expected, input.ToSlug());
    }

    [Fact]
    public void ToSlug_WithEmptyString_ShouldReturnEmpty()
    {
        Assert.Equal("", "".ToSlug());
    }

    #endregion

    #region ToTitleCase Tests

    [Theory]
    [InlineData("hello world", "Hello World")]
    [InlineData("HELLO WORLD", "Hello World")]
    [InlineData("test", "Test")]
    public void ToTitleCase_ShouldConvertCorrectly(string input, string expected)
    {
        Assert.Equal(expected, input.ToTitleCase());
    }

    [Fact]
    public void ToTitleCase_WithNullOrWhitespace_ShouldReturnInput()
    {
        Assert.Null(((string)null!).ToTitleCase());
        Assert.Equal("", "".ToTitleCase());
        Assert.Equal("   ", "   ".ToTitleCase());
    }

    #endregion

    #region Truncate Tests

    [Fact]
    public void Truncate_ShouldTruncateWithSuffix()
    {
        var result = "This is a long text".Truncate(10);
        Assert.Equal("This is...", result);
        Assert.Equal(10, result.Length);
    }

    [Fact]
    public void Truncate_ShortText_ShouldReturnOriginal()
    {
        Assert.Equal("Short", "Short".Truncate(100));
    }

    [Fact]
    public void Truncate_ExactLength_ShouldReturnOriginal()
    {
        Assert.Equal("12345", "12345".Truncate(5));
    }

    [Fact]
    public void Truncate_WithCustomSuffix_ShouldUseIt()
    {
        Assert.Equal("Hello…", "Hello World".Truncate(6, "…"));
    }

    [Fact]
    public void Truncate_NullOrEmpty_ShouldReturnAsIs()
    {
        Assert.Null(((string)null!).Truncate(10));
        Assert.Equal("", "".Truncate(10));
    }

    #endregion

    #region NullIfEmpty Tests

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("\t\n", null)]
    public void NullIfEmpty_WithEmptyOrWhitespace_ShouldReturnNull(string? input, string? expected)
    {
        Assert.Equal(expected, input.NullIfEmpty());
    }

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("  hello  ", "hello")]
    public void NullIfEmpty_WithValue_ShouldReturnTrimmed(string input, string expected)
    {
        Assert.Equal(expected, input.NullIfEmpty());
    }

    #endregion
}
