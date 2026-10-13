using System.Text.Json;
using CustomDataView.Core.Connections.Calls;
using CustomDataView.Core.Connections.Definitions;

namespace CustomDataView.Core.Connections.Validation;

public static class ParameterValuesValidator
{
    public static IDictionary<string, string[]> Validate(ConnectionDefinition definition, ParameterValuesInput values)
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var parameter in definition.PathParameters)
        {
            if (values.Path.TryGetValue(parameter.Name, out var value))
            {
                if (value.ValueKind != JsonValueKind.String || value.GetString() == string.Empty)
                {
                    errors[$"parameters.path.{parameter.Name}"] = ["Wartość parametru adresu musi być niepustym tekstem."];
                }
            }
            else if (parameter.Required)
            {
                errors[$"parameters.path.{parameter.Name}"] = ["Wartość wymaganego parametru jest wymagana."];
            }
        }

        AddUndeclared("path", definition.PathParameters, values.Path, errors);

        foreach (var parameter in definition.QueryParameters)
        {
            if (values.Query.TryGetValue(parameter.Name, out var value))
            {
                if (!IsQueryValue(value))
                {
                    errors[$"parameters.query.{parameter.Name}"] = ["Wartość parametru zapytania musi być tekstem albo niepustą listą tekstów."];
                }
            }
            else if (parameter.Required)
            {
                errors[$"parameters.query.{parameter.Name}"] = ["Wartość wymaganego parametru jest wymagana."];
            }
        }

        AddUndeclared("query", definition.QueryParameters, values.Query, errors);
        var bodyParameters = (definition as PostConnectionDefinition)?.BodyParameters ?? [];
        foreach (var parameter in bodyParameters.Where(parameter => parameter.Required && !values.Body.ContainsKey(parameter.Name)))
        {
            errors[$"parameters.body.{parameter.Name}"] = ["Wartość wymaganego parametru jest wymagana."];
        }

        return errors;
    }

    private static void AddUndeclared(
        string section,
        IReadOnlyList<ParameterDeclaration> parameters,
        IReadOnlyDictionary<string, JsonElement> values,
        Dictionary<string, string[]> errors)
    {
        var declared = parameters.Select(parameter => parameter.Name).ToHashSet();
        foreach (var name in values.Keys.Where(name => !declared.Contains(name)))
        {
            errors[$"parameters.{section}.{name}"] = ["Parametr nie został zadeklarowany w połączeniu."];
        }
    }

    private static bool IsQueryValue(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.String
            || (value.ValueKind == JsonValueKind.Array
                && value.GetArrayLength() > 0
                && value.EnumerateArray().All(item => item.ValueKind == JsonValueKind.String));
    }
}
