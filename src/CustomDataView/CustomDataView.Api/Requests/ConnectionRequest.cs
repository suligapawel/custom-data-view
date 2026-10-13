using System.Text.Json;
using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Validation;

namespace CustomDataView.Api.Requests;

public sealed record ConnectionRequest(
    string Name,
    string Method,
    string Url,
    IReadOnlyList<ParameterDeclarationInput> PathParameters,
    JsonElement? Body,
    IReadOnlyList<ParameterDeclarationInput> BodyParameters)
{
    public IDictionary<string, string[]> Validate()
    {
        return ConnectionDefinitionValidator.Validate(ToInput());
    }

    public ConnectionInput ToInput()
    {
        return new ConnectionInput(Name, Method, Url, PathParameters ?? [], Body, BodyParameters ?? []);
    }
}
