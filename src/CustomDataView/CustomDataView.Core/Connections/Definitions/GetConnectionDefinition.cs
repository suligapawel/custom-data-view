namespace CustomDataView.Core.Connections.Definitions;

public sealed record GetConnectionDefinition(
    string Name,
    string Url,
    IReadOnlyList<NameValuePair> Headers,
    IReadOnlyList<ParameterDeclaration> PathParameters,
    IReadOnlyList<ParameterDeclaration> QueryParameters)
    : ConnectionDefinition(Name, Url, Headers, PathParameters, QueryParameters);
