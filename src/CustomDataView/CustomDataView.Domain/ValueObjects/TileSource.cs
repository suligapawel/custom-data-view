namespace CustomDataView.Domain.ValueObjects;

public sealed record TileSource(string Key, string Kind, Guid? ConnectionId);
