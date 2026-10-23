namespace CustomDataView.Application.Tiles.Dtos;

public sealed record TileFieldDto(
    string Source,
    string Path,
    string Label,
    IReadOnlyList<TransformationStepDto> Transformations,
    bool Hidden,
    string Role);
