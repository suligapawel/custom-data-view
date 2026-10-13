using System.Text.Json;

namespace CustomDataView.Core.Connections.Definitions;

public sealed record ConnectionInput(
    string Name,
    string Method,
    string Url,
    IReadOnlyList<NameValuePair> Headers,
    IReadOnlyList<ParameterDeclarationInput> PathParameters,
    IReadOnlyList<ParameterDeclarationInput> QueryParameters,
    JsonElement? Body,
    IReadOnlyList<ParameterDeclarationInput> BodyParameters)
{
    public ConnectionDefinition ToDefinition()
    {
        var pathParameters = PathParameters.Select(ToDeclaration).ToList();
        var queryParameters = QueryParameters.Select(ToDeclaration).ToList();
        if (string.Equals(Method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return new PostConnectionDefinition(
                Name,
                Url,
                Headers,
                pathParameters,
                queryParameters,
                RawJson(Body),
                BodyParameters.Select(ToDeclaration).ToList());
        }

        return new GetConnectionDefinition(Name, Url, Headers, pathParameters, queryParameters);
    }

    private static ParameterDeclaration ToDeclaration(ParameterDeclarationInput parameter)
    {
        return new ParameterDeclaration(parameter.Name, parameter.Required, RawJson(parameter.DefaultValue));
    }

    private static string RawJson(JsonElement? element)
    {
        return element is { ValueKind: not JsonValueKind.Null } value ? value.GetRawText() : null;
    }
}
