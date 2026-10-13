using System.Text.Json;
using CustomDataView.Core.Connections.Definitions;

namespace CustomDataView.Api.Requests;

public sealed record ConnectionRequest(string Name, string Method, JsonElement? Body)
{
    public ConnectionDefinition ToDefinition()
    {
        if (Method == "POST")
        {
            var body = Body is { ValueKind: not JsonValueKind.Null } element ? element.GetRawText() : null;
            return new PostConnectionDefinition(Name, body);
        }

        return new GetConnectionDefinition(Name);
    }
}
