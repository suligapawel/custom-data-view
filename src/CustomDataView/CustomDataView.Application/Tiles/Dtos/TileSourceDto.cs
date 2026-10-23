namespace CustomDataView.Application.Tiles.Dtos;

public sealed record TileSourceDto(string Key, string Kind, Guid? ConnectionId);
