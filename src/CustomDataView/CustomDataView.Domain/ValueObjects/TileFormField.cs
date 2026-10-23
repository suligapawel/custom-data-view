using System.Text.Json.Nodes;

namespace CustomDataView.Domain.ValueObjects;

public sealed record TileFormField(string Label, string Type, bool Required, JsonNode Default);
