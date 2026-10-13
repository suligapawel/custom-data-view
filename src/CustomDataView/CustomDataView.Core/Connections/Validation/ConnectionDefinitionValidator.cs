using CustomDataView.Core.Connections.Definitions;

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

        return errors;
    }
}
