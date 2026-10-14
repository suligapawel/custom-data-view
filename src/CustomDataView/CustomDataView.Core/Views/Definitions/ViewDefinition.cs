namespace CustomDataView.Core.Views.Definitions;

public sealed record ViewDefinition(string Name, IReadOnlyList<ViewTile> Tiles);
