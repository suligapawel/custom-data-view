namespace CustomDataView.Application.Tiles.Inputs;

public sealed record TilePaginationInput(string PageNumberParameter, string PageSizeParameter, int? PageSize, string TotalPath);
