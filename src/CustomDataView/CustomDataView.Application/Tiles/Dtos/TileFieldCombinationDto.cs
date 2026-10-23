namespace CustomDataView.Application.Tiles.Dtos;

public sealed record TileFieldCombinationDto(string Key, string Parameter, IReadOnlyList<string> Fields);
