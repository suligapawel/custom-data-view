using CustomDataView.Application.Tiles.Inputs;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Services;

namespace CustomDataView.Application.Tiles.Validation;

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

        var labels = new HashSet<string>();
        for (var index = 0; index < input.Fields.Count; index++)
        {
            ValidateField(input.Fields[index], $"fields[{index}]", errors);
            if (IsValidText(input.Fields[index].Label) && !labels.Add(TileLabels.Normalize(input.Fields[index].Label)))
            {
                errors[$"fields[{index}].label"] = ["Etykieta pola musi być unikalna w kafelku."];
            }
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

        for (var position = 0; position < field.Transformations.Count; position++)
        {
            foreach (var (name, value) in field.Transformations[position].Parameters)
            {
                if (!TransformationParameterValues.IsStorable(value))
                {
                    errors[$"{key}.transformations[{position}].parameters.{name}"] =
                        ["Wartość parametru transformacji musi być tekstem lub liczbą."];
                }
            }
        }
    }

    private static bool IsValidText(string text) => !string.IsNullOrWhiteSpace(text) && text.Length <= MaxTextLength;
}
