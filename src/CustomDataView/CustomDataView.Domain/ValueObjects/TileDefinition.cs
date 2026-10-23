namespace CustomDataView.Domain.ValueObjects;

public sealed record TileDefinition(string Name, IReadOnlyList<TileField> Fields);
