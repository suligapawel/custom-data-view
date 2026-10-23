namespace CustomDataView.Domain.ValueObjects;

public sealed record TileField(
    string Source,
    string Path,
    string Label,
    IReadOnlyList<TransformationStep> Transformations,
    bool Hidden = false,
    string Role = null);
