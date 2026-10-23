namespace CustomDataView.Domain.ValueObjects;

public sealed record TileAction(string Key, string Label, string Type, Guid TileId, string Scope, IReadOnlyList<TileActionMapping> Map);
