using CustomDataView.Core.Tiles.Definitions;
using CustomDataView.Core.Tiles.Paths;

namespace CustomDataView.Core.Tiles.Validation;

public static class TileDefinitionValidator
{
    private const int MaxTextLength = 100;

    public static IDictionary<string, string[]> Validate(TileInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (!IsValidText(input.Name))
        {
            errors["name"] = [$"Nazwa kafelka jest wymagana i może mieć najwyżej {MaxTextLength} znaków."];
        }

        if (input.Fields.Count == 0)
        {
            errors["fields"] = ["Kafelek musi mieć co najmniej jedno pole."];
        }

        for (var index = 0; index < input.Fields.Count; index++)
        {
            ValidateField(input.Fields[index], $"fields[{index}]", errors);
        }

        return errors;
    }

    private static void ValidateField(TileFieldInput field, string key, Dictionary<string, string[]> errors)
    {
        if (!IsValidText(field.Label))
        {
            errors[$"{key}.label"] = [$"Etykieta pola jest wymagana i może mieć najwyżej {MaxTextLength} znaków."];
        }

        if (field.ConnectionId is null)
        {
            errors[$"{key}.connectionId"] = ["Połączenie pola jest wymagane."];
        }

        if (!JsonPathParser.TryParse(field.Path, out _))
        {
            errors[$"{key}.path"] = [$"Ścieżka pola {key} jest wymagana i musi być poprawną ścieżką JSONPath."];
        }
    }

    private static bool IsValidText(string text) => !string.IsNullOrWhiteSpace(text) && text.Length <= MaxTextLength;
}
