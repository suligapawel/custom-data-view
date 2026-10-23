namespace CustomDataView.Application.Tiles.Dtos;

public sealed record TileFieldDto(
    Guid ConnectionId,
    string Path,
    string Label,
    IReadOnlyList<TransformationStepDto> Transformations);
