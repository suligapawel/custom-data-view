using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Core.Transformations.Abstractions;

public static class TransformationValue
{
    private const string NumberTextFormat = "0.############################";

    public static object FromJson(JsonNode node)
        => node?.GetValueKind() switch
        {
            JsonValueKind.Number => (object)node.GetValue<decimal>(),
            JsonValueKind.String => node.GetValue<string>(),
            null => null,
            _ => node
        };

    public static JsonNode ToJson(object value)
        => value switch
        {
            decimal number => JsonValue.Create(number),
            JsonNode node => node.DeepClone(),
            null => null,
            _ => JsonValue.Create((string)value)
        };

    public static decimal ToNumber(string key, object value)
    {
        if (value is decimal number)
        {
            return number;
        }

        if (value is string text
            && decimal.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new TransformationFailedException(key, TransformationFailureReasons.InvalidValue);
    }

    public static string ToText(string key, object value)
    {
        if (value is decimal number)
        {
            return number.ToString(NumberTextFormat, CultureInfo.InvariantCulture);
        }

        if (value is JsonNode)
        {
            throw new TransformationFailedException(key, TransformationFailureReasons.InvalidValue);
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }
}
