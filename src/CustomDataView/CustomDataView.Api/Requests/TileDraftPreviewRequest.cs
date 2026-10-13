namespace CustomDataView.Api.Requests;

public sealed record TileDraftPreviewRequest(TileRequest Tile, IReadOnlyList<ConnectionValuesRequest> Connections);
