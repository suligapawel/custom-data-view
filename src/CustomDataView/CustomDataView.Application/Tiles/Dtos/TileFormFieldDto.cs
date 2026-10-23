using System.Text.Json.Nodes;

namespace CustomDataView.Application.Tiles.Dtos;

public sealed record TileFormFieldDto(string Label, string Type, bool Required, JsonNode Default);
