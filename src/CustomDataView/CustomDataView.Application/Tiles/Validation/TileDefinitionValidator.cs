using CustomDataView.Application.Tiles.Inputs;
using CustomDataView.Domain.Dictionaries;
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

        var sources = ValidateSources(input.Sources, errors);
        if (input.Fields.Count == 0)
        {
            errors["fields"] = ["Kafelek musi mieć co najmniej jedno pole."];
        }

        var labels = new HashSet<string>();
        for (var index = 0; index < input.Fields.Count; index++)
        {
            ValidateField(input.Fields[index], $"fields[{index}]", sources, errors);
            if (IsValidText(input.Fields[index].Label) && !labels.Add(TileLabels.Normalize(input.Fields[index].Label)))
            {
                errors[$"fields[{index}].label"] = ["Etykieta pola musi być unikalna w kafelku."];
            }
        }

        return errors;
    }

    private static HashSet<string> ValidateSources(IReadOnlyList<TileSourceInput> sources, Dictionary<string, string[]> errors)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < sources.Count; index++)
        {
            var source = sources[index];
            var key = $"sources[{index}]";
            if (!TileNodeKeys.IsValid(source.Key))
            {
                errors[$"{key}.key"] =
                [
                    $"Klucz źródła jest wymagany, może mieć najwyżej {TileNodeKeys.MaxLength} znaków, nie może zawierać znaku '/' ani być słowem '{TileNodeKeys.Parent}'."
                ];
            }
            else if (!keys.Add(source.Key))
            {
                errors[$"{key}.key"] = [$"Klucz źródła '{source.Key}' występuje w kafelku więcej niż raz."];
            }

            if (source.Kind != TileSourceKinds.Connection)
            {
                errors[$"{key}.kind"] = [$"Rodzaj źródła musi mieć wartość '{TileSourceKinds.Connection}'."];
            }
            else if (source.ConnectionId is null)
            {
                errors[$"{key}.connectionId"] = ["Połączenie źródła jest wymagane."];
            }
        }

        return keys;
    }

    private static void ValidateField(TileFieldInput field, string key, HashSet<string> sources, Dictionary<string, string[]> errors)
    {
        if (!IsValidText(field.Label))
        {
            errors[$"{key}.label"] = [$"Etykieta pola jest wymagana i może mieć najwyżej {MaxTextLength} znaków."];
        }

        if (string.IsNullOrWhiteSpace(field.Source))
        {
            errors[$"{key}.source"] = ["Źródło pola jest wymagane."];
        }
        else if (!sources.Contains(field.Source))
        {
            errors[$"{key}.source"] = [$"Źródło '{field.Source}' nie istnieje w kafelku."];
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
