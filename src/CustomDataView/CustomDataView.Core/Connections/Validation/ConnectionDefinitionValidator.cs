using System.Text.Json;
using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Templates;

namespace CustomDataView.Core.Connections.Validation;

public static class ConnectionDefinitionValidator
{
    private const int MaxNameLength = 100;
    private const string RequiredWithDefaultMessage = "Parametr wymagany nie może mieć wartości domyślnej.";
    private const string RepeatedNameMessage = "Nazwa parametru powtarza się.";
    private const string SnakeCaseMessage = "Nazwa parametru musi być w formacie snake_case i mieć najwyżej 50 znaków.";

    public static IDictionary<string, string[]> Validate(ConnectionInput input)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateName(input, errors);
        ValidateMethod(input, errors);
        ValidateUrl(input, errors);
        ValidateHeaders(input, errors);
        ValidateQueryParameters(input, errors);
        ValidateBody(input, errors);
        return errors;
    }

    private static void ValidateName(ConnectionInput input, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > MaxNameLength)
        {
            errors["name"] = [$"Nazwa połączenia jest wymagana i może mieć najwyżej {MaxNameLength} znaków."];
        }
    }

    private static void ValidateMethod(ConnectionInput input, Dictionary<string, string[]> errors)
    {
        if (!ConnectionMethods.IsGet(input.Method) && !ConnectionMethods.IsPost(input.Method))
        {
            errors["method"] = ["Rodzaj połączenia musi być GET albo POST."];
        }
    }

    private static void ValidateUrl(ConnectionInput input, Dictionary<string, string[]> errors)
    {
        if (!Uri.TryCreate(input.Url, UriKind.Absolute, out var url)
            || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
            || UrlTemplate.HasPlaceholdersOutsidePath(input.Url)
            || UrlTemplate.HasUnclosedBraces(input.Url))
        {
            errors["url"] = ["Adres połączenia musi być bezwzględnym adresem http lub https z parametrami tylko w ścieżce."];
            return;
        }

        var placeholders = UrlTemplate.PlaceholderNames(input.Url);
        var undeclared = Undeclared(placeholders, input.PathParameters);
        if (undeclared.Count > 0)
        {
            errors["url"] = undeclared.Select(name => $"Parametr adresu \"{name}\" nie został zadeklarowany.").ToArray();
        }

        for (var index = 0; index < input.PathParameters.Count; index++)
        {
            var key = $"pathParameters[{index}]";
            ValidateTemplateParameterName(input.PathParameters, index, placeholders, key, errors, "Parametr adresu nie występuje w adresie.");

            var parameter = input.PathParameters[index];
            if (parameter.Required && parameter.DefaultValue is not null)
            {
                errors[$"{key}.defaultValue"] = [RequiredWithDefaultMessage];
            }
            else if (!parameter.Required
                && (parameter.DefaultValue is not { ValueKind: JsonValueKind.String } defaultValue
                    || defaultValue.GetString() == string.Empty))
            {
                errors[$"{key}.defaultValue"] = ["Parametr adresu musi być wymagany albo mieć niepustą wartość domyślną."];
            }
        }
    }

    private static void ValidateHeaders(ConnectionInput input, Dictionary<string, string[]> errors)
    {
        for (var index = 0; index < input.Headers.Count; index++)
        {
            if (!ParameterNames.IsHeaderToken(input.Headers[index].Name))
            {
                errors[$"headers[{index}].name"] = ["Nazwa nagłówka jest wymagana i nie może zawierać spacji ani dwukropka."];
            }

            if (input.Headers[index].Value is null)
            {
                errors[$"headers[{index}].value"] = ["Wartość nagłówka jest wymagana (może być pusta)."];
            }
        }
    }

    private static void ValidateQueryParameters(ConnectionInput input, Dictionary<string, string[]> errors)
    {
        for (var index = 0; index < input.QueryParameters.Count; index++)
        {
            var parameter = input.QueryParameters[index];
            if (string.IsNullOrEmpty(parameter.Name))
            {
                errors[$"queryParameters[{index}].name"] = ["Nazwa parametru zapytania jest wymagana."];
            }

            if (parameter.Required && parameter.DefaultValue is not null)
            {
                errors[$"queryParameters[{index}].defaultValue"] = [RequiredWithDefaultMessage];
            }
            else if (parameter.DefaultValue is { ValueKind: not JsonValueKind.String })
            {
                errors[$"queryParameters[{index}].defaultValue"] = ["Wartość domyślna parametru zapytania musi być tekstem."];
            }
        }
    }

    private static void ValidateBody(ConnectionInput input, Dictionary<string, string[]> errors)
    {
        var hasBody = input.Body is { ValueKind: not JsonValueKind.Null };
        if (ConnectionMethods.IsGet(input.Method))
        {
            if (hasBody)
            {
                errors["body"] = ["Treść jest dozwolona tylko dla połączenia POST."];
            }

            if (input.BodyParameters.Count > 0)
            {
                errors["bodyParameters"] = ["Parametry treści są dozwolone tylko dla połączenia POST."];
            }

            return;
        }

        var placeholders = hasBody ? BodyTemplate.PlaceholderNames(input.Body!.Value) : [];
        var undeclared = Undeclared(placeholders, input.BodyParameters);
        if (undeclared.Count > 0)
        {
            errors["body"] = undeclared.Select(name => $"Parametr treści \"{name}\" nie został zadeklarowany.").ToArray();
        }

        for (var index = 0; index < input.BodyParameters.Count; index++)
        {
            ValidateTemplateParameterName(
                input.BodyParameters,
                index,
                placeholders,
                $"bodyParameters[{index}]",
                errors,
                "Parametr treści nie występuje w treści.");

            var parameter = input.BodyParameters[index];
            if (parameter.Required && parameter.DefaultValue is not null)
            {
                errors[$"bodyParameters[{index}].defaultValue"] = [RequiredWithDefaultMessage];
            }
        }
    }

    private static void ValidateTemplateParameterName(
        IReadOnlyList<ParameterDeclarationInput> parameters,
        int index,
        IReadOnlyList<string> placeholders,
        string key,
        Dictionary<string, string[]> errors,
        string unusedMessage)
    {
        var name = parameters[index].Name;
        if (!ParameterNames.IsSnakeCase(name))
        {
            errors[$"{key}.name"] = [SnakeCaseMessage];
        }
        else if (parameters.Take(index).Any(parameter => parameter.Name == name))
        {
            errors[$"{key}.name"] = [RepeatedNameMessage];
        }
        else if (!placeholders.Contains(name))
        {
            errors[$"{key}.name"] = [unusedMessage];
        }
    }

    private static List<string> Undeclared(IReadOnlyList<string> placeholders, IReadOnlyList<ParameterDeclarationInput> parameters)
    {
        var declared = parameters.Select(parameter => parameter.Name).ToHashSet();
        return placeholders.Where(name => !declared.Contains(name)).ToList();
    }
}
