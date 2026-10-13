using System.Text.Json;

namespace CustomDataView.Core.Connections.Definitions;

public sealed record ConnectionInput(
    string Name,
    string Method,
    string Url,
    IReadOnlyList<ParameterDeclarationInput> PathParameters,
    JsonElement? Body,
    IReadOnlyList<ParameterDeclarationInput> BodyParameters)
{
    public ConnectionDefinition ToDefinition()
    {
        if (string.Equals(Method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            var body = Body is { ValueKind: not JsonValueKind.Null } element ? element.GetRawText() : null;
            return new PostConnectionDefinition(Name, Url, body);
        }

        return new GetConnectionDefinition(Name, Url);
    }
}
