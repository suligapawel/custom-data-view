namespace CustomDataView.Core.Tiles.Definitions;

public sealed record TileFieldInput(
    Guid? ConnectionId,
    string Path,
    string Label,
    IReadOnlyList<TransformationStepInput> Transformations);
