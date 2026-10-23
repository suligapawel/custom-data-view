namespace CustomDataView.Application.Tiles.Inputs;

public sealed record TileFieldInput(
    string Source,
    string Path,
    string Label,
    IReadOnlyList<TransformationStepInput> Transformations,
    bool Hidden = false);
