using System.Text.Json;
using CustomDataView.Core.Connections.Calls;
using CustomDataView.Core.Connections.Definitions;

namespace CustomDataView.Core.Connections.Validation;

public static class ParameterValuesValidator
{
    private const string MissingValueMessage = "Wartość wymaganego parametru jest wymagana.";

    public static IDictionary<string, string[]> Validate(ConnectionDefinition definition, ParameterValuesInput values)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateSection(
            "path",
            definition.PathParameters,
            values.Path,
            value => value.ValueKind == JsonValueKind.String && value.GetString() != string.Empty,
            "Wartość parametru adresu musi być niepustym tekstem.",
            errors);
        ValidateSection(
            "query",
            definition.QueryParameters,
            values.Query,
            IsQueryValue,
            "Wartość parametru zapytania musi być tekstem albo niepustą listą tekstów.",
            errors);
        ValidateSection(
            "body",
            (definition as PostConnectionDefinition)?.BodyParameters ?? [],
            values.Body,
            _ => true,
            null,
            errors);
        return errors;
    }

    private static void ValidateSection(
        string section,
        IReadOnlyList<ParameterDeclaration> parameters,
        IReadOnlyDictionary<string, JsonElement> values,
        Func<JsonElement, bool> isValid,
        string invalidValueMessage,
        Dictionary<string, string[]> errors)
    {
        foreach (var parameter in parameters)
        {
            var key = $"parameters.{section}.{parameter.Name}";
            if (values.TryGetValue(parameter.Name, out var value))
            {
                if (!isValid(value))
                {
                    errors[key] = [invalidValueMessage];
                }
            }
            else if (parameters.Any(declaration => declaration.Name == parameter.Name && declaration.Required))
            {
                errors[key] = [MissingValueMessage];
            }
        }

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
