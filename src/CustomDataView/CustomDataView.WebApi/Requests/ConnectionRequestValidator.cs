using System.Text.Json;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Services;

namespace CustomDataView.WebApi.Requests;

public static class ConnectionRequestValidator
{
    private const int MaxNameLength = 100;
    private const string RequiredWithDefaultMessage = "Parametr wymagany nie może mieć wartości domyślnej.";
    private const string RepeatedNameMessage = "Nazwa parametru powtarza się.";
    private const string SnakeCaseMessage = "Nazwa parametru musi być w formacie snake_case i mieć najwyżej 50 znaków.";

    public static IDictionary<string, string[]> Validate(ConnectionRequest request)
    {
        var normalized = request with
        {
            PathParameters = request.PathParameters ?? [],
            QueryParameters = request.QueryParameters ?? [],
            BodyParameters = request.BodyParameters ?? []
        };
        var errors = new Dictionary<string, string[]>();
        ValidateName(normalized, errors);
        ValidateMethod(normalized, errors);
        ValidateUrl(normalized, errors);
        ValidateQueryParameters(normalized, errors);
        ValidateBody(normalized, errors);
        return errors;
    }

    private static void ValidateName(ConnectionRequest request, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > MaxNameLength)
        {
            errors["name"] = [$"Nazwa połączenia jest wymagana i może mieć najwyżej {MaxNameLength} znaków."];
        }
    }

    private static void ValidateMethod(ConnectionRequest request, Dictionary<string, string[]> errors)
    {
        if (!ConnectionMethods.IsGet(request.Method) && !ConnectionMethods.IsPost(request.Method))
        {
            errors["method"] = ["Rodzaj połączenia musi być GET albo POST."];
        }
    }

    private static void ValidateUrl(ConnectionRequest request, Dictionary<string, string[]> errors)
    {
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var url)
            || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
            || UrlTemplate.HasPlaceholdersOutsidePath(request.Url)
            || UrlTemplate.HasUnclosedBraces(request.Url))
        {
            errors["url"] = ["Adres połączenia musi być bezwzględnym adresem http lub https z parametrami tylko w ścieżce."];
            return;
        }

        var placeholders = UrlTemplate.PlaceholderNames(request.Url);
        var undeclared = Undeclared(placeholders, request.PathParameters);
        if (undeclared.Count > 0)
        {
            errors["url"] = undeclared.Select(name => $"Parametr adresu \"{name}\" nie został zadeklarowany.").ToArray();
        }

        for (var index = 0; index < request.PathParameters.Count; index++)
        {
            var key = $"pathParameters[{index}]";
            ValidateTemplateParameterName(request.PathParameters, index, placeholders, key, errors, "Parametr adresu nie występuje w adresie.");

            var parameter = request.PathParameters[index];
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

    private static void ValidateQueryParameters(ConnectionRequest request, Dictionary<string, string[]> errors)
    {
        for (var index = 0; index < request.QueryParameters.Count; index++)
        {
            var parameter = request.QueryParameters[index];
            if (string.IsNullOrEmpty(parameter.Name))
            {
                errors[$"queryParameters[{index}].name"] = ["Nazwa parametru zapytania jest wymagana."];
            }
            else if (request.QueryParameters.Take(index).Any(previous => previous.Name == parameter.Name))
            {
                errors[$"queryParameters[{index}].name"] = [RepeatedNameMessage];
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

    private static void ValidateBody(ConnectionRequest request, Dictionary<string, string[]> errors)
    {
        var hasBody = request.Body is { ValueKind: not JsonValueKind.Null };
        if (ConnectionMethods.IsGet(request.Method))
        {
            if (hasBody)
            {
                errors["body"] = ["Treść jest dozwolona tylko dla połączenia POST."];
            }

            if (request.BodyParameters.Count > 0)
            {
                errors["bodyParameters"] = ["Parametry treści są dozwolone tylko dla połączenia POST."];
            }

            return;
        }

        var placeholders = hasBody ? BodyTemplate.PlaceholderNames(request.Body!.Value) : [];
        var undeclared = Undeclared(placeholders, request.BodyParameters);
        if (undeclared.Count > 0)
        {
            errors["body"] = undeclared.Select(name => $"Parametr treści \"{name}\" nie został zadeklarowany.").ToArray();
        }

        for (var index = 0; index < request.BodyParameters.Count; index++)
        {
            ValidateTemplateParameterName(
                request.BodyParameters,
                index,
                placeholders,
                $"bodyParameters[{index}]",
                errors,
                "Parametr treści nie występuje w treści.");

            var parameter = request.BodyParameters[index];
            if (parameter.Required && parameter.DefaultValue is not null)
            {
                errors[$"bodyParameters[{index}].defaultValue"] = [RequiredWithDefaultMessage];
            }
        }
    }

    private static void ValidateTemplateParameterName(
        IReadOnlyList<ParameterDeclarationRequest> parameters,
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

    private static List<string> Undeclared(IReadOnlyList<string> placeholders, IReadOnlyList<ParameterDeclarationRequest> parameters)
    {
        var declared = parameters.Select(parameter => parameter.Name).ToHashSet();
        return placeholders.Where(name => !declared.Contains(name)).ToList();
    }
}
