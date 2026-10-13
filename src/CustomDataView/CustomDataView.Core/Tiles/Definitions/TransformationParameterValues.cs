using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CustomDataView.Core.Transformations.Abstractions;

namespace CustomDataView.Core.Tiles.Definitions;

public static partial class TransformationParameterValues
{
    public static bool IsStorable(JsonNode value)
        => value?.GetValueKind() is JsonValueKind.String or JsonValueKind.Number;

    public static string ToText(JsonNode value)
        => value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : value.ToJsonString();

    public static JsonNode FromText(ITransformation transformation, string name, string text)
    {
        var type = transformation?.Parameters.FirstOrDefault(parameter => parameter.Name == name)?.Type;
        if (type == TransformationParameterTypes.Integer
            && IntegerPattern().IsMatch(text)
            && int.TryParse(text, out var number))
        {
            return JsonValue.Create(number);
        }

        return JsonValue.Create(text);
    }

    [GeneratedRegex("^-?[0-9]+$")]
    private static partial Regex IntegerPattern();
}
