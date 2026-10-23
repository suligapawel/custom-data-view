using System.Text.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Application.Tiles.Preview;

public sealed record TilePreviewValues(
    IReadOnlyDictionary<string, JsonElement> Parameters,
    IReadOnlyDictionary<string, JsonElement> Inputs)
{
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, JsonElement>> Forms { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, JsonElement>>();

    public IReadOnlyDictionary<string, TilePreviewValues> Descendants { get; init; } = new Dictionary<string, TilePreviewValues>();

    public static TilePreviewValues Of(IReadOnlyDictionary<string, JsonElement> parameters)
        => new(parameters, new Dictionary<string, JsonElement>());

    public IReadOnlyDictionary<string, JsonElement> Form(string address)
        => Forms.GetValueOrDefault(address) ?? new Dictionary<string, JsonElement>();

    public TileValues ToValues()
        => new()
        {
            Parameters = Nodes(Parameters),
            Inputs = Nodes(Inputs),
            Descendants = Descendants.ToDictionary(pair => pair.Key, pair => pair.Value.ToValues())
        };

    private static Dictionary<string, JsonNode> Nodes(IReadOnlyDictionary<string, JsonElement> values)
        => values.ToDictionary(pair => pair.Key, pair => JsonNode.Parse(pair.Value.GetRawText()));
}
