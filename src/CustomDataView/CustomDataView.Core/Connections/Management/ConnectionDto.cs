using System.Text.Json.Nodes;
using CustomDataView.Core.Connections.Definitions;

namespace CustomDataView.Core.Connections.Management;

public sealed record ConnectionDto(
    Guid Id,
    string Name,
    string Method,
    string Url,
    IReadOnlyList<NameValuePair> Headers,
    IReadOnlyList<ParameterDeclarationDto> PathParameters,
    IReadOnlyList<ParameterDeclarationDto> QueryParameters,
    JsonNode Body,
    IReadOnlyList<ParameterDeclarationDto> BodyParameters);
