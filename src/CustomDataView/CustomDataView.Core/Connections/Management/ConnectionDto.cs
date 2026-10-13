using System.Text.Json.Nodes;

namespace CustomDataView.Core.Connections.Management;

public sealed record ConnectionDto(
    Guid Id,
    string Name,
    string Method,
    string Url,
    IReadOnlyList<ParameterDeclarationDto> PathParameters,
    IReadOnlyList<ParameterDeclarationDto> QueryParameters,
    JsonNode Body,
    IReadOnlyList<ParameterDeclarationDto> BodyParameters);
