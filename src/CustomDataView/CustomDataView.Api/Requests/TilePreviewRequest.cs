using System.Text.Json;

namespace CustomDataView.Api.Requests;

public sealed record TilePreviewRequest(Dictionary<string, JsonElement> Parameters)
{
    public IReadOnlyDictionary<string, JsonElement> ToInput() => ValuesOf(Parameters);

    public static IReadOnlyDictionary<string, JsonElement> ValuesOf(Dictionary<string, JsonElement> parameters)
        => parameters ?? new Dictionary<string, JsonElement>();
}
