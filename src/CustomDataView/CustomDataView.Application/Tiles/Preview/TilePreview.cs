namespace CustomDataView.Application.Tiles.Preview;

public sealed record TilePreview(Guid? Id, string Name, IReadOnlyList<TileFieldPreview> Fields);
