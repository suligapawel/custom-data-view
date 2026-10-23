namespace CustomDataView.Application.Tiles.Dtos;

public sealed record TilePaginationDto(string PageNumberParameter, string PageSizeParameter, int PageSize, string TotalPath);
