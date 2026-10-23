namespace CustomDataView.Domain.ValueObjects;

public sealed record TileFieldCombination(string Key, string Parameter, IReadOnlyList<string> Fields);
