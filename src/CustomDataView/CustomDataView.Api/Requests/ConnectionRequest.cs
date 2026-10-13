using System.Text.Json;
using CustomDataView.Core.Connections.Definitions;

namespace CustomDataView.Api.Requests;

public sealed record ConnectionRequest(
    string Name,
    string Method,
    string Url,
    IReadOnlyList<ParameterDeclarationRequest> PathParameters,
    IReadOnlyList<ParameterDeclarationRequest> QueryParameters,
    JsonElement? Body,
    IReadOnlyList<ParameterDeclarationRequest> BodyParameters)
{
    public IDictionary<string, string[]> Validate() => ConnectionRequestValidator.Validate(this);

    public ConnectionDefinition ToDefinition()
    {
        var pathParameters = ToDeclarations(PathParameters);
        var queryParameters = ToDeclarations(QueryParameters);

        return ConnectionMethods.IsGet(Method)
            ? new GetConnectionDefinition(Name, Url, pathParameters, queryParameters)
            : new PostConnectionDefinition(
                Name,
                Url,
                pathParameters,
                queryParameters,
                RawJson(Body),
                ToDeclarations(BodyParameters));
    }

    private static List<ParameterDeclaration> ToDeclarations(IReadOnlyList<ParameterDeclarationRequest> parameters)
        => (parameters ?? [])
            .Select(parameter => new ParameterDeclaration(
                parameter.Name,
                parameter.Required,
                RawJson(parameter.DefaultValue)))
            .ToList();

    private static string RawJson(JsonElement? element)
        => element is { ValueKind: not JsonValueKind.Null } value ? value.GetRawText() : null;
}
