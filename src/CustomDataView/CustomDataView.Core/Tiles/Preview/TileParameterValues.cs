using System.Text.Json;
using CustomDataView.Core.Connections.Calls;
using CustomDataView.Core.Connections.Definitions;

namespace CustomDataView.Core.Tiles.Preview;

public static class TileParameterValues
{
    public static ParameterValuesInput For(
        ConnectionDefinition definition,
        IReadOnlyDictionary<string, JsonElement> parameters)
        => new ParameterValuesInput(
            Declared(definition.PathParameters, parameters),
            Declared(definition.QueryParameters, parameters),
            Declared((definition as PostConnectionDefinition)?.BodyParameters ?? [], parameters));

    private static Dictionary<string, JsonElement> Declared(
        IReadOnlyList<ParameterDeclaration> declarations,
        IReadOnlyDictionary<string, JsonElement> parameters)
        => declarations
            .Where(declaration => parameters.ContainsKey(declaration.Name))
            .ToDictionary(declaration => declaration.Name, declaration => parameters[declaration.Name]);
}
