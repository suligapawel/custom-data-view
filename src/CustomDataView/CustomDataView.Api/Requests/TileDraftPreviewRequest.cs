namespace CustomDataView.Api.Requests;

public sealed record TileDraftPreviewRequest(TileRequest Tile, IReadOnlyList<ConnectionValuesRequest> Connections)
{
    public IDictionary<string, string[]> Validate()
        => Tile is null
            ? new Dictionary<string, string[]> { ["tile"] = ["Definicja kafelka jest wymagana."] }
            : Tile.Validate().ToDictionary(error => $"tile.{error.Key}", error => error.Value);
}
