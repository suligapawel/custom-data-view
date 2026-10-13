using System.Text.Json;

namespace CustomDataView.Api.Requests;

public sealed record ConnectionRequest(JsonElement? Body)
{
    public string BodyText => Body is { ValueKind: not JsonValueKind.Null } body ? body.GetRawText() : null;
}
