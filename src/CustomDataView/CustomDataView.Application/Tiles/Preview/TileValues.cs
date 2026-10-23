using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CustomDataView.Application.Tiles.Preview;

public sealed record TileValues
{
    public static readonly TileValues Empty = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, JsonNode> Parameters { get; init; } = new Dictionary<string, JsonNode>();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, JsonNode> Inputs { get; init; } = new Dictionary<string, JsonNode>();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, TileValues> Descendants { get; init; } = new Dictionary<string, TileValues>();

    [JsonIgnore]
    public IReadOnlySet<string> Bound { get; init; } = new HashSet<string>();

    public TileValues DescendantsOf(string key)
    {
        var own = Descendants.GetValueOrDefault(key) ?? Empty;
        var descendants = own.Descendants.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        foreach (var (path, values) in Descendants.Where(pair => pair.Key.StartsWith($"{key}/", StringComparison.Ordinal)))
        {
            descendants[path[(key.Length + 1)..]] = values;
        }

        return own with { Descendants = descendants };
    }

    public TileValues Compact()
        => new()
        {
            Parameters = Parameters?.Count > 0 ? Parameters : null,
            Inputs = Inputs?.Count > 0 ? Inputs : null,
            Descendants = Descendants?.Count > 0 ? Descendants.ToDictionary(pair => pair.Key, pair => pair.Value.Compact()) : null
        };
}
