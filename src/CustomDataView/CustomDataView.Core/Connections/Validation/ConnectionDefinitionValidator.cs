using System.Text.Json;
using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Templates;

namespace CustomDataView.Core.Connections.Validation;

public static class ConnectionDefinitionValidator
{
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

        if (!IsSupportedMethod(input.Method))
        {
            errors["method"] = ["Rodzaj połączenia musi być GET albo POST."];
        }

        if (IsGet(input.Method) && input.Body is { ValueKind: not JsonValueKind.Null })
        {
            errors["body"] = ["Treść jest dozwolona tylko dla połączenia POST."];
        }

        if (IsGet(input.Method) && input.BodyParameters.Count > 0)
        {
            errors["bodyParameters"] = ["Parametry treści są dozwolone tylko dla połączenia POST."];
        }

        return errors;
    }

    private static bool IsSupportedMethod(string method)
    {
        return IsGet(method) || string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGet(string method)
    {
        return string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase);
    }
}
