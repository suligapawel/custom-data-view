namespace CustomDataView.Application.Tiles.Inputs;

public sealed record TileChildInput(string Key, Guid? TileId, int? Position, string CssClass, string Scope, string Load, string Label = null);
