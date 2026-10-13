namespace CustomDataView.Core.Connections.Definitions;

public sealed record PostConnectionDefinition(
    string Name,
    string Url,
    IReadOnlyList<NameValuePair> Headers,
    IReadOnlyList<ParameterDeclaration> PathParameters,
    IReadOnlyList<ParameterDeclaration> QueryParameters,
    string Body,
    IReadOnlyList<ParameterDeclaration> BodyParameters)
    : ConnectionDefinition(Name, Url, Headers, PathParameters, QueryParameters);
