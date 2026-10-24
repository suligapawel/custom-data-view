using System.Text.Json;
using CustomDataView.Domain.Dictionaries;

namespace CustomDataView.WebApi.Requests;

public static class ConnectionRequestValidator
{
    public static IDictionary<string, string[]> Validate(ConnectionRequest request)
    {
        var errors = new Dictionary<string, string[]>(request.ToDefinition().Check());
        if (!ConnectionMethods.IsGet(request.Method) && !ConnectionMethods.IsPost(request.Method))
        {
            errors["method"] = ["Rodzaj połączenia musi być GET albo POST."];
        }

        if (ConnectionMethods.IsGet(request.Method))
        {
            if (request.Body is { ValueKind: not JsonValueKind.Null })
            {
                errors["body"] = ["Treść jest dozwolona tylko dla połączenia POST."];
            }

            if (request.BodyParameters?.Count > 0)
            {
                errors["bodyParameters"] = ["Parametry treści są dozwolone tylko dla połączenia POST."];
            }
        }

        return errors;
    }
}
