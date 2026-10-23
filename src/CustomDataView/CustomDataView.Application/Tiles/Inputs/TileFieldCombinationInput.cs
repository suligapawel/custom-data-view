using System.Text.Json.Nodes;

namespace CustomDataView.Application.Tiles.Inputs;

public sealed record TileFieldCombinationInput(string Key, JsonNode Parameter, IReadOnlyList<string> Fields);
