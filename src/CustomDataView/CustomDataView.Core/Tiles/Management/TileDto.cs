namespace CustomDataView.Core.Tiles.Management;

public sealed record TileDto(Guid Id, string Name, IReadOnlyList<TileFieldDto> Fields);
