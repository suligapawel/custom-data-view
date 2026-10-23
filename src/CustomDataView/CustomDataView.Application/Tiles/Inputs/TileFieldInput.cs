namespace CustomDataView.Application.Tiles.Inputs;

public sealed record TileFieldInput(
    Guid? ConnectionId,
    string Path,
    string Label,
    IReadOnlyList<TransformationStepInput> Transformations,
    bool Hidden = false);
