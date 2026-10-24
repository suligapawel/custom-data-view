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

    public static bool TryBuild(IEnumerable<(string Key, JsonNode Value)> entries, out MapperDictionary dictionary)
    {
        dictionary = null;
        var collected = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
        foreach (var (key, value) in entries)
        {
            if (!collected.TryAdd(key, value))
            {
                return false;
            }
        }

        dictionary = new MapperDictionary(collected.ToFrozenDictionary(StringComparer.Ordinal));
        return true;
    }

    public bool TryGet(string key, out object value)
    {
        var found = entries.TryGetValue(key, out var node);
        value = found ? TransformationValue.FromJson(node?.DeepClone()) : null;
        return found;
    }
}
