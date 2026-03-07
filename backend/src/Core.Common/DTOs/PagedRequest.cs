namespace Core.Common.DTOs;

/// <summary>
/// Standard pagination request parameters for list endpoints.
/// </summary>
public class PagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
    public string? Search { get; set; }

    public const int MaxPageSize = 100;

    public int GetValidatedPageSize() => Math.Clamp(PageSize, 1, MaxPageSize);
    public int GetValidatedPage() => Math.Max(Page, 1);
    public int GetSkip() => (GetValidatedPage() - 1) * GetValidatedPageSize();
}
