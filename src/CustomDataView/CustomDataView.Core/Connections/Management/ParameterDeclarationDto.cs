using System.Text.Json.Nodes;

namespace CustomDataView.Core.Connections.Management;

public sealed record ParameterDeclarationDto(string Name, bool Required, JsonNode DefaultValue);
