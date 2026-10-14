using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace CustomDataView.Api.Requests;

public sealed record TilePreviewRequest(Dictionary<string, JsonElement> Parameters)
{
    public IReadOnlyDictionary<string, JsonElement> ToInput() => ValuesOf(Parameters);

    public static IReadOnlyDictionary<string, JsonElement> ValuesOf(Dictionary<string, JsonElement> parameters)
        => parameters ?? new Dictionary<string, JsonElement>();

    public static IReadOnlyDictionary<string, JsonElement> ValuesOf(IQueryCollection query)
        => query.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Count == 1
                ? JsonSerializer.SerializeToElement(pair.Value.ToString())
                : JsonSerializer.SerializeToElement(pair.Value.ToArray()));
}
