using CustomDataView.Core.Transformations.Execution;

namespace CustomDataView.Core.Tiles.Definitions;

public sealed record TileField(
    Guid ConnectionId,
    string Path,
    string Label,
    IReadOnlyList<TransformationStep> Transformations);
