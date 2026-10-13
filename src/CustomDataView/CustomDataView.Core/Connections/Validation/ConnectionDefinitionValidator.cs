using System.Text.Json;
using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Templates;

namespace CustomDataView.Core.Connections.Validation;

public static class ConnectionDefinitionValidator
{
    private const string RepeatedNameMessage = "Nazwa parametru powtarza się.";
    private const string SnakeCaseMessage = "Nazwa parametru musi być w formacie snake_case i mieć najwyżej 50 znaków.";

    public static IDictionary<string, string[]> Validate(ConnectionInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 100)
        {
            errors["name"] = ["Nazwa połączenia jest wymagana."];
        }

        if (!Uri.TryCreate(input.Url, UriKind.Absolute, out var url)
            || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
            || UrlTemplate.HasPlaceholdersOutsidePath(input.Url)
            || UrlTemplate.HasUnclosedBraces(input.Url))
        {
            errors["url"] = ["Adres połączenia musi być bezwzględnym adresem http lub https."];
        }
        else
        {
            var placeholders = UrlTemplate.PlaceholderNames(input.Url);
            var declared = input.PathParameters.Select(parameter => parameter.Name).ToHashSet();
            var undeclared = placeholders.Where(name => !declared.Contains(name)).ToList();
            if (undeclared.Count > 0)
            {
                errors["url"] = undeclared.Select(name => $"Parametr adresu \"{name}\" nie został zadeklarowany.").ToArray();
            }

            for (var index = 0; index < input.PathParameters.Count; index++)
            {
                if (!ParameterNames.IsSnakeCase(input.PathParameters[index].Name))
                {
                    errors[$"pathParameters[{index}].name"] = [SnakeCaseMessage];
                }
                else if (IsRepeated(input.PathParameters, index))
                {
                    errors[$"pathParameters[{index}].name"] = [RepeatedNameMessage];
                }
                else if (!placeholders.Contains(input.PathParameters[index].Name))
                {
                    errors[$"pathParameters[{index}].name"] = ["Parametr adresu nie występuje w adresie."];
                }

                var parameter = input.PathParameters[index];
                if (!parameter.Required
                    && (parameter.DefaultValue is not { ValueKind: JsonValueKind.String } defaultValue
                        || defaultValue.GetString() == string.Empty))
                {
                    errors[$"pathParameters[{index}].defaultValue"] =
                        ["Parametr adresu musi być wymagany albo mieć niepustą wartość domyślną."];
                }
            }
        }

        if (!IsSupportedMethod(input.Method))
        {
            errors["method"] = ["Rodzaj połączenia musi być GET albo POST."];
        }

        for (var index = 0; index < input.Headers.Count; index++)
        {
            if (!ParameterNames.IsHeaderToken(input.Headers[index].Name))
            {
                errors[$"headers[{index}].name"] = ["Nazwa nagłówka jest wymagana i nie może zawierać spacji ani dwukropka."];
            }
        }

        for (var index = 0; index < input.QueryParameters.Count; index++)
        {
            if (string.IsNullOrEmpty(input.QueryParameters[index].Name))
            {
                errors[$"queryParameters[{index}].name"] = ["Nazwa parametru zapytania jest wymagana."];
            }

            ValidateDefaultValue(input.QueryParameters[index], $"queryParameters[{index}]", errors);
        }

        if (IsGet(input.Method) && input.Body is { ValueKind: not JsonValueKind.Null })
        {
            errors["body"] = ["Treść jest dozwolona tylko dla połączenia POST."];
        }

        for (var index = 0; index < input.Headers.Count; index++)
        {
            if (!ParameterNames.IsHeaderToken(input.Headers[index].Name))
            {
                errors[$"headers[{index}].name"] = ["Nazwa nagłówka jest wymagana i nie może zawierać spacji ani dwukropka."];
            }
        }

        for (var index = 0; index < input.QueryParameters.Count; index++)
        {
            if (string.IsNullOrEmpty(input.QueryParameters[index].Name))
            {
                errors[$"queryParameters[{index}].name"] = ["Nazwa parametru zapytania jest wymagana."];
            }

            ValidateDefaultValue(input.QueryParameters[index], $"queryParameters[{index}]", errors);
        }

        if (IsGet(input.Method) && input.BodyParameters.Count > 0)
        {
            errors["bodyParameters"] = ["Parametry treści są dozwolone tylko dla połączenia POST."];
        }
        else if (!errors.ContainsKey("body"))
        {
            ValidateBodyParameters(input, errors);
        }

        return errors;
    }

    private static void ValidateDefaultValue(
        ParameterDeclarationInput parameter,
        string key,
        Dictionary<string, string[]> errors)
    {
        if (parameter.Required && parameter.DefaultValue is not null)
        {
            errors[$"{key}.defaultValue"] = ["Parametr wymagany nie może mieć wartości domyślnej."];
        }
    }

    private static bool IsRepeated(IReadOnlyList<ParameterDeclarationInput> parameters, int index)
    {
        return parameters.Take(index).Any(parameter => parameter.Name == parameters[index].Name);
    }

    private static bool IsSupportedMethod(string method)
    {
        return IsGet(method) || string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGet(string method)
    {
        return string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateBodyParameters(ConnectionInput input, Dictionary<string, string[]> errors)
    {
        var placeholders = input.Body is { ValueKind: not JsonValueKind.Null } body
            ? BodyTemplate.PlaceholderNames(body)
            : [];
        var declared = input.BodyParameters.Select(parameter => parameter.Name).ToHashSet();
        var undeclared = placeholders.Where(name => !declared.Contains(name)).ToList();
        if (undeclared.Count > 0)
        {
            errors["body"] = undeclared.Select(name => $"Parametr treści \"{name}\" nie został zadeklarowany.").ToArray();
        }

        for (var index = 0; index < input.BodyParameters.Count; index++)
        {
            if (!ParameterNames.IsSnakeCase(input.BodyParameters[index].Name))
            {
                errors[$"bodyParameters[{index}].name"] = [SnakeCaseMessage];
            }
            else if (IsRepeated(input.BodyParameters, index))
            {
                errors[$"bodyParameters[{index}].name"] = [RepeatedNameMessage];
            }
            else if (!placeholders.Contains(input.BodyParameters[index].Name))
            {
                errors[$"bodyParameters[{index}].name"] = ["Parametr treści nie występuje w treści."];
            }
        }
    }
}
