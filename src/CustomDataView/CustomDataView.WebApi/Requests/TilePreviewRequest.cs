using System.Text.Json;
using CustomDataView.Application.Tiles.Preview;
using Microsoft.AspNetCore.Http;

namespace CustomDataView.WebApi.Requests;

public sealed record TilePreviewRequest(
    Dictionary<string, JsonElement> Parameters,
    Dictionary<string, JsonElement> Inputs,
    Dictionary<string, Dictionary<string, JsonElement>> Forms)
{
    public IReadOnlyDictionary<string, JsonElement> ToInput() => ValuesOf(Parameters);

    public TilePreviewValues ToValues() => ValuesOf(Parameters, Inputs, Forms);

    public static TilePreviewValues ValuesOf(
        Dictionary<string, JsonElement> parameters,
        Dictionary<string, JsonElement> inputs,
        Dictionary<string, Dictionary<string, JsonElement>> forms)
        => new(ValuesOf(parameters), ValuesOf(inputs))
        {
            Forms = (forms ?? []).ToDictionary(
                form => form.Key,
                form => (IReadOnlyDictionary<string, JsonElement>)(form.Value ?? new Dictionary<string, JsonElement>()))
        };

    public static IReadOnlyDictionary<string, JsonElement> ValuesOf(Dictionary<string, JsonElement> parameters)
        => parameters ?? new Dictionary<string, JsonElement>();

    public static IReadOnlyDictionary<string, JsonElement> ValuesOf(IQueryCollection query)
        => query.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Count == 1
                ? JsonSerializer.SerializeToElement(pair.Value.ToString())
                : JsonSerializer.SerializeToElement(pair.Value.ToArray()));
}
