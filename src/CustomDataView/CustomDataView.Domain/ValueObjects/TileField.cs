namespace CustomDataView.Domain.ValueObjects;

public sealed record TileField(
    Guid ConnectionId,
    string Path,
    string Label,
    IReadOnlyList<TransformationStep> Transformations);
