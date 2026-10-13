using System.Text.Json.Nodes;

namespace CustomDataView.Core.Transformations.Chain;

public sealed record TransformationStep(string Key, IReadOnlyDictionary<string, JsonNode> Parameters);
