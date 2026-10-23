using System.Text.Json.Nodes;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Exceptions;

namespace CustomDataView.Domain.ValueObjects;

public sealed class TransformationParameters
{
    private readonly IReadOnlyDictionary<string, JsonNode> _values;

    public TransformationParameters(IReadOnlyDictionary<string, JsonNode> values)
    {
        _values = values ?? new Dictionary<string, JsonNode>();
    }

    public int GetInteger(string key, string name) => GetRequired(key, name).GetValue<int>();

    public string GetText(string key, string name) => GetRequired(key, name).GetValue<string>();

    private JsonNode GetRequired(string key, string name)
    {
        if (!_values.TryGetValue(name, out var value))
        {
            throw new TransformationFailedException(key, TransformationFailureReasons.InvalidParameter, name);
        }

        return value;
    }
}
