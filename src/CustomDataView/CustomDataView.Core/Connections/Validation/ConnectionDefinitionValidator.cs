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

        return errors;
    }
}
