using System.Text.Json.Serialization;

namespace CustomDataView.Application.Tiles.Dtos;

public sealed record TileDto(
    Guid Id,
    string Name,
    string Display,
    IReadOnlyList<TileSourceDto> Sources,
    TileItemsDto Items,
    IReadOnlyList<object> Fields,
    IReadOnlyList<TileChildDto> Children,
    IReadOnlyList<TileBindingDto> Bindings,
    TileContractDto Contract)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TilePaginationDto Pagination { get; init; }

    public IReadOnlyList<TileActionDto> Actions { get; init; } = [];
}
