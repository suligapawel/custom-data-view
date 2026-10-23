namespace CustomDataView.Domain.ValueObjects;

public sealed record TilePagination(string PageNumberParameter, string PageSizeParameter, int PageSize, string TotalPath);
