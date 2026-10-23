namespace CustomDataView.Domain.ValueObjects;

public sealed record TileChild(string Key, Guid TileId, int Position, string CssClass, string Scope, string Load);
