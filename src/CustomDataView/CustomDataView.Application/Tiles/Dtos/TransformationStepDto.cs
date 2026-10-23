using System.Text.Json.Nodes;

namespace CustomDataView.Application.Tiles.Dtos;

public sealed record TransformationStepDto(string Key, IReadOnlyDictionary<string, JsonNode> Parameters);
