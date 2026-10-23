using System.Text.Json.Nodes;

namespace CustomDataView.Application.Tiles.Inputs;

public sealed record TransformationStepInput(string Key, IReadOnlyDictionary<string, JsonNode> Parameters);
