using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Entities;

public sealed record Tile(Guid Id, TileDefinition Definition);
