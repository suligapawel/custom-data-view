namespace CustomDataView.Domain.ValueObjects;

public sealed record ViewDefinition(string Name, IReadOnlyList<ViewTile> Tiles);
