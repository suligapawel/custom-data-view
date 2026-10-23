using System.Text.Json;
using CustomDataView.Application.Connections.Calls;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Preview;

public static class TileParameterValues
{
    public static ParameterValuesInput For(
        ConnectionDefinition definition,
        IReadOnlyDictionary<string, JsonElement> parameters)
        => new ParameterValuesInput(
            Declared(definition.PathParameters, parameters, AsText),
            Declared(definition.QueryParameters, parameters, AsText),
            Declared((definition as PostConnectionDefinition)?.BodyParameters ?? [], parameters, value => value));

    private static Dictionary<string, JsonElement> Declared(
        IReadOnlyList<ParameterDeclaration> declarations,
        IReadOnlyDictionary<string, JsonElement> parameters,
        Func<JsonElement, JsonElement> convert)
        => declarations
            .Where(declaration => parameters.ContainsKey(declaration.Name))
            .ToDictionary(declaration => declaration.Name, declaration => convert(parameters[declaration.Name]));

    private static JsonElement AsText(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => JsonSerializer.SerializeToElement(value.GetRawText()),
            JsonValueKind.Array => JsonSerializer.SerializeToElement(value.EnumerateArray().Select(AsText).ToList()),
            _ => value
        };
}
