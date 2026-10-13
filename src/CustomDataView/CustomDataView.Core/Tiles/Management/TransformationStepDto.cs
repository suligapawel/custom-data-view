using System.Text.Json.Nodes;

namespace CustomDataView.Core.Tiles.Management;

public sealed record TransformationStepDto(string Key, IReadOnlyDictionary<string, JsonNode> Parameters);
