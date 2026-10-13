using CustomDataView.Core.Tiles.Definitions;

namespace CustomDataView.Api.Requests;

public sealed record TileRequest(string Name, IReadOnlyList<TileFieldRequest> Fields)
{
    public TileDefinition ToDefinition()
        => new TileDefinition(
            Name,
            Fields.Select(field => new TileField(field.ConnectionId, field.Path, field.Label, [])).ToList());
}

public sealed record TileFieldRequest(Guid ConnectionId, string Path, string Label);
