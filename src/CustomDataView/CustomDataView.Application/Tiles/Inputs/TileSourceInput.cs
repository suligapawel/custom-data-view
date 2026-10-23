namespace CustomDataView.Application.Tiles.Inputs;

public sealed record TileSourceInput(string Key, string Kind, Guid? ConnectionId);
