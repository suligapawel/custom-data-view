using System.Text.Json;
using CustomDataView.Core.Connections.Definitions;

namespace CustomDataView.Api.Requests;

public sealed record ConnectionRequest(
    string Name,
    string Method,
    string Url,
    IReadOnlyList<NameValuePair> Headers,
    IReadOnlyList<ParameterDeclarationRequest> PathParameters,
    IReadOnlyList<ParameterDeclarationRequest> QueryParameters,
    JsonElement? Body,
    IReadOnlyList<ParameterDeclarationRequest> BodyParameters)
{
    public IDictionary<string, string[]> Validate()
    {
        return ConnectionRequestValidator.Validate(this);
    }

    public ConnectionDefinition ToDefinition()
    {
        var headers = Headers ?? [];
        var pathParameters = ToDeclarations(PathParameters);
        var queryParameters = ToDeclarations(QueryParameters);
        if (ConnectionMethods.IsPost(Method))
        {
            return new PostConnectionDefinition(
                Name,
                Url,
                headers,
                pathParameters,
                queryParameters,
                RawJson(Body),
                ToDeclarations(BodyParameters));
        }

        return new GetConnectionDefinition(Name, Url, headers, pathParameters, queryParameters);
    }

    private static List<ParameterDeclaration> ToDeclarations(IReadOnlyList<ParameterDeclarationRequest> parameters)
    {
        return (parameters ?? [])
            .Select(parameter => new ParameterDeclaration(parameter.Name, parameter.Required, RawJson(parameter.DefaultValue)))
            .ToList();
    }

    private static string RawJson(JsonElement? element)
    {
        return element is { ValueKind: not JsonValueKind.Null } value ? value.GetRawText() : null;
    }
}
