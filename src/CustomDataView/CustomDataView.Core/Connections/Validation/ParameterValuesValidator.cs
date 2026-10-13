using CustomDataView.Core.Connections.Calls;
using CustomDataView.Core.Connections.Definitions;

namespace CustomDataView.Core.Connections.Validation;

public static class ParameterValuesValidator
{
    public static IDictionary<string, string[]> Validate(ConnectionDefinition definition, ConnectionParameterValues values)
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var parameter in definition.PathParameters)
        {
            if (parameter.Required && !values.Path.ContainsKey(parameter.Name))
            {
                errors[$"parameters.path.{parameter.Name}"] = ["Wartość wymaganego parametru jest wymagana."];
            }
        }

        return errors;
    }
}
