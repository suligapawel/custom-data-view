namespace CustomDataView.Application.Tiles.Dtos;

public sealed record TileDto(Guid Id, string Name, IReadOnlyList<TileFieldDto> Fields);
