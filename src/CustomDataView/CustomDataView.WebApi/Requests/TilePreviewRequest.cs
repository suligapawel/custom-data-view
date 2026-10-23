using System.Text.Json;
using CustomDataView.Application.Tiles.Preview;
using Microsoft.AspNetCore.Http;

namespace CustomDataView.WebApi.Requests;

public sealed record TilePreviewRequest(Dictionary<string, JsonElement> Parameters, Dictionary<string, JsonElement> Inputs)
{
    public IReadOnlyDictionary<string, JsonElement> ToInput() => ValuesOf(Parameters);

    public TilePreviewValues ToValues() => ValuesOf(Parameters, Inputs);

    public static TilePreviewValues ValuesOf(Dictionary<string, JsonElement> parameters, Dictionary<string, JsonElement> inputs)
        => new(ValuesOf(parameters), ValuesOf(inputs));

    public static IReadOnlyDictionary<string, JsonElement> ValuesOf(Dictionary<string, JsonElement> parameters)
        => parameters ?? new Dictionary<string, JsonElement>();

    public static IReadOnlyDictionary<string, JsonElement> ValuesOf(IQueryCollection query)
        => query.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Count == 1
                ? JsonSerializer.SerializeToElement(pair.Value.ToString())
                : JsonSerializer.SerializeToElement(pair.Value.ToArray()));
}
