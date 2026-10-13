using System.Globalization;
using System.Text.Json.Nodes;

namespace CustomDataView.Core.Transformations;

public static class TransformationValue
{
    private const string NumberTextFormat = "0.############################";

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
