using System.Text.Json.Nodes;

namespace CustomDataView.Domain.ValueObjects;

public sealed record TransformationStep(string Key, IReadOnlyDictionary<string, JsonNode> Parameters);
