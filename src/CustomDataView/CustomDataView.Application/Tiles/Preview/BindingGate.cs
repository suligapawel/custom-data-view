using System.Text.Json.Nodes;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Preview;

public sealed class BindingGate
{
    public Dictionary<string, JsonNode> Parameters { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, JsonNode> Inputs { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, BindingGateValues> Descendants { get; } = new(StringComparer.Ordinal);

    public List<TilePendingBinding> Pending { get; } = [];

    public TileFieldErrorCause Failure { get; set; }

    public bool NotLoaded { get; set; }

    public void Set(string path, TileBindingMapping mapping, JsonNode value)
    {
        var parameters = Parameters;
        var inputs = Inputs;
        if (path is not null)
        {
            if (!Descendants.TryGetValue(path, out var values))
            {
                Descendants[path] = values = new BindingGateValues();
            }

            parameters = values.Parameters;
            inputs = values.Inputs;
        }

        if (mapping.Parameter is not null)
        {
            parameters[mapping.Parameter] = value;
        }
        else
        {
            inputs[mapping.Input] = value;
        }
    }
}

public sealed class BindingGateValues
{
    public Dictionary<string, JsonNode> Parameters { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, JsonNode> Inputs { get; } = new(StringComparer.Ordinal);
}
