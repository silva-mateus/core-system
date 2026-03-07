using System.Text.RegularExpressions;

namespace Core.Common.Validation;

/// <summary>
/// Validates and formats Brazilian CPF and CNPJ documents.
/// </summary>
public static partial class DocumentValidator
{
    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigitRegex();

    /// <summary>
    /// Strips all non-digit characters from a document string.
    /// </summary>
    public static string StripFormatting(string document)
        => NonDigitRegex().Replace(document, "");

    /// <summary>
    /// Validates a CPF or CNPJ document (auto-detected by length).
    /// Returns true for null/empty input (use [Required] separately).
    /// </summary>
    public static bool IsValidDocument(string? document)
    {
        if (string.IsNullOrWhiteSpace(document)) return true;

        var digits = StripFormatting(document);

        return digits.Length switch
        {
            11 => IsValidCpf(digits),
            14 => IsValidCnpj(digits),
            _ => false
        };
    }

    /// <summary>
    /// Validates a CPF (11-digit Brazilian individual document) using check digits.
    /// </summary>
    public static bool IsValidCpf(string digits)
    {
        if (digits.Length != 11 || digits.Distinct().Count() == 1) return false;

        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (digits[i] - '0') * (10 - i);
        var remainder = sum % 11;
        var first = remainder < 2 ? 0 : 11 - remainder;
        if (digits[9] - '0' != first) return false;

        sum = 0;
        for (var i = 0; i < 10; i++)
            sum += (digits[i] - '0') * (11 - i);
        remainder = sum % 11;
        var second = remainder < 2 ? 0 : 11 - remainder;
        return digits[10] - '0' == second;
    }

    /// <summary>
    /// Validates a CNPJ (14-digit Brazilian company document) using check digits.
    /// </summary>
    public static bool IsValidCnpj(string digits)
    {
        if (digits.Length != 14 || digits.Distinct().Count() == 1) return false;

        int[] weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] weights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += (digits[i] - '0') * weights1[i];
        var remainder = sum % 11;
        var first = remainder < 2 ? 0 : 11 - remainder;
        if (digits[12] - '0' != first) return false;

        sum = 0;
        for (var i = 0; i < 13; i++)
            sum += (digits[i] - '0') * weights2[i];
        remainder = sum % 11;
        var second = remainder < 2 ? 0 : 11 - remainder;
        return digits[13] - '0' == second;
    }
}
