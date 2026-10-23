namespace CustomDataView.Application.Tiles.Dtos;

public sealed record TileChildDto(string Key, Guid TileId, int Position, string CssClass, string Scope, string Load);
