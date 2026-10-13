using System.Text.Json.Nodes;

namespace CustomDataView.Core.Tiles.Definitions;

public sealed record TransformationStepInput(string Key, IReadOnlyDictionary<string, JsonNode> Parameters);
