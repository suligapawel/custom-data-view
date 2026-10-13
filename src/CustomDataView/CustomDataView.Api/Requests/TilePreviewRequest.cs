using System.Text.Json;
using CustomDataView.Core.Connections.Calls;
using CustomDataView.Core.Tiles.Preview;

namespace CustomDataView.Api.Requests;

public sealed record TilePreviewRequest(IReadOnlyList<ConnectionValuesRequest> Connections)
{
    public IReadOnlyList<ConnectionValuesInput> ToInput() => ValuesOf(Connections);

    public static IReadOnlyList<ConnectionValuesInput> ValuesOf(IReadOnlyList<ConnectionValuesRequest> connections)
        => (connections ?? [])
            .Select(connection => new ConnectionValuesInput(
                connection.ConnectionId,
                new ParameterValuesInput(
                    connection.Path ?? new Dictionary<string, JsonElement>(),
                    connection.Query ?? new Dictionary<string, JsonElement>(),
                    connection.Body ?? new Dictionary<string, JsonElement>())))
            .ToList();
}

public sealed record ConnectionValuesRequest(
    Guid? ConnectionId,
    Dictionary<string, JsonElement> Path,
    Dictionary<string, JsonElement> Query,
    Dictionary<string, JsonElement> Body);
