using System.Text.Json.Nodes;

namespace CustomDataView.Core.Transformations;

public sealed class TransformationParameters
{
    private readonly IReadOnlyDictionary<string, JsonNode> _values;

    public TransformationParameters(IReadOnlyDictionary<string, JsonNode> values)
    {
        _values = values ?? new Dictionary<string, JsonNode>();
    }

    public int GetInteger(string name)
    {
        return _values[name].GetValue<int>();
    }
}
