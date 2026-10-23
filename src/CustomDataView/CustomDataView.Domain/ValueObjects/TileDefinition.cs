namespace CustomDataView.Domain.ValueObjects;

public sealed record TileDefinition(string Name, IReadOnlyList<TileSource> Sources, IReadOnlyList<TileField> Fields);
