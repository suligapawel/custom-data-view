using System.Text.Json.Nodes;

namespace CustomDataView.Core.Transformations;

public sealed class TransformationParameters
{
    private readonly IReadOnlyDictionary<string, JsonNode> _values;

    public TransformationParameters(IReadOnlyDictionary<string, JsonNode> values)
    {
        _values = values ?? new Dictionary<string, JsonNode>();
    }

    public int GetInteger(string key, string name)
    {
        if (!_values.TryGetValue(name, out var value))
        {
            throw new TransformationFailedException(key, TransformationFailureReasons.InvalidParameter, name);
        }

        return value.GetValue<int>();
    }

    public string GetText(string key, string name)
    {
        return _values[name].GetValue<string>();
    }
}
