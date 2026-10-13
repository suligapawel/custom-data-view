using CustomDataView.Core.Tiles.Definitions;

namespace CustomDataView.Api.Requests;

public sealed record TileRequest(string Name)
{
    public TileDefinition ToDefinition() => new TileDefinition(Name);
}
