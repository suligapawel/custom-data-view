namespace CustomDataView.Core.Connections.Definitions;

public sealed record GetConnectionDefinition(
    string Name,
    string Url,
    IReadOnlyList<ParameterDeclaration> PathParameters,
    IReadOnlyList<ParameterDeclaration> QueryParameters)
    : ConnectionDefinition(Name, Url, PathParameters, QueryParameters);
