using System.Text.Json.Nodes;

namespace CustomDataView.Application.Tiles.Inputs;

public sealed record TileFieldInput(
    string Source,
    string Path,
    string Label,
    IReadOnlyList<TransformationStepInput> Transformations,
    bool Hidden = false,
    string Role = null)
{
    public string Type { get; init; }

    public bool Required { get; init; }

    public JsonNode Default { get; init; }

    public TileFieldCombinationInput Combine { get; init; }
}
