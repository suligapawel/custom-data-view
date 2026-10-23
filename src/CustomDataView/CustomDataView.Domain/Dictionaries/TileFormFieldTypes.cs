using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Domain.Dictionaries;

public static class TileFormFieldTypes
{
    public const string Text = "text";
    public const string Number = "number";
    public const string Boolean = "boolean";
    public const string Date = "date";

    public static readonly IReadOnlyList<string> All = [Text, Number, Boolean, Date];

    public static bool Matches(string type, JsonNode value)
    {
        if (value is not JsonValue scalar)
        {
            return false;
        }

        var kind = scalar.GetValueKind();
        return type switch
        {
            Text => kind == JsonValueKind.String,
            Number => kind == JsonValueKind.Number,
            Boolean => kind is JsonValueKind.True or JsonValueKind.False,
            Date => kind == JsonValueKind.String
                && DateOnly.TryParseExact(scalar.GetValue<string>(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            _ => false
        };
    }
}
