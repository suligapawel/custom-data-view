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
            if (values.Path.TryGetValue(parameter.Name, out var value))
            {
                if (string.IsNullOrEmpty(value))
                {
                    errors[$"parameters.path.{parameter.Name}"] = ["Wartość parametru adresu nie może być pusta."];
                }
            }
            else if (parameter.Required)
            {
                errors[$"parameters.path.{parameter.Name}"] = ["Wartość wymaganego parametru jest wymagana."];
            }
        }

        var declared = definition.PathParameters.Select(parameter => parameter.Name).ToHashSet();
        foreach (var name in values.Path.Keys.Where(name => !declared.Contains(name)))
        {
            errors[$"parameters.path.{name}"] = ["Parametr nie został zadeklarowany w połączeniu."];
        }

        return errors;
    }
}
