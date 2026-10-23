using System.Text.Json.Nodes;

namespace CustomDataView.Application.Connections.Dtos;

public sealed record ParameterDeclarationDto(string Name, bool Required, JsonNode DefaultValue);
