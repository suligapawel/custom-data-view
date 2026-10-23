using System.Text.Json.Nodes;

namespace CustomDataView.Domain.ValueObjects;

public sealed record CombinationInput(string Label, JsonNode Value);
