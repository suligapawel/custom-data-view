using CustomDataView.Application.Tiles.Contracts;

namespace CustomDataView.Application.Tiles.Dtos;

public sealed record TileDto(
    Guid Id,
    string Name,
    string Display,
    IReadOnlyList<TileSourceDto> Sources,
    TileItemsDto Items,
    IReadOnlyList<TileFieldDto> Fields,
    IReadOnlyList<TileBindingDto> Bindings,
    TileContract Contract);
