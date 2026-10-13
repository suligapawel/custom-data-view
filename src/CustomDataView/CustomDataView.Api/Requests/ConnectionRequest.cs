using System.Text.Json;
using CustomDataView.Core.Connections.Definitions;

namespace CustomDataView.Api.Requests;

public sealed record ConnectionRequest(string Name, JsonElement? Body)
{
    public ConnectionDefinition ToDefinition()
    {
        var body = Body is { ValueKind: not JsonValueKind.Null } element ? element.GetRawText() : null;
        return new ConnectionDefinition(Name, body);
    }
}
