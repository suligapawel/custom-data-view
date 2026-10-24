using System.Collections.Frozen;
using System.Text.Json.Nodes;
using CustomDataView.Domain.Services.Transformations;

namespace CustomDataView.Domain.ValueObjects;

public sealed class MapperDictionary
{
    private readonly FrozenDictionary<string, JsonNode> entries;

    private MapperDictionary(FrozenDictionary<string, JsonNode> entries)
    {
        this.entries = entries;
    }

    public static MapperDictionary Build(IEnumerable<(string Key, JsonNode Value)> entries)
        => new(entries.ToFrozenDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal));

    public bool TryGet(string key, out object value)
    {
        var found = entries.TryGetValue(key, out var node);
        value = found ? TransformationValue.FromJson(node?.DeepClone()) : null;
        return found;
    }
}
