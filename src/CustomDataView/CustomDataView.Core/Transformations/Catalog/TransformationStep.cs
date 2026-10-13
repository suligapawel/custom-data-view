using System.Text.Json.Nodes;

namespace CustomDataView.Core.Transformations.Catalog;

public sealed record TransformationStep(string Key, IReadOnlyDictionary<string, JsonNode> Parameters);
