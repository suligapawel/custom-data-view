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

        var displayIsValid = TileDisplays.All.Contains(input.Display) && input.Display != TileDisplays.Form;
        if (!displayIsValid)
        {
            errors["display"] =
            [
                $"Typ wyświetlania jest wymagany i musi mieć jedną z wartości: {string.Join(", ", TileDisplays.All.Where(display => display != TileDisplays.Form))}."
            ];
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

        if (displayIsValid)
        {
            ValidateRoles(input, errors);
            ValidateItems(input, sources, errors);
        }

        TileBindingsValidator.Validate(input, errors);

        return errors;
    }

    private static void ValidateRoles(TileInput input, Dictionary<string, string[]> errors)
    {
        var allowed = TileFieldRoles.For(input.Display);
        for (var index = 0; index < input.Fields.Count; index++)
        {
            var role = input.Fields[index].Role;
            if (!string.IsNullOrWhiteSpace(role) && !allowed.Contains(role))
            {
                errors[$"fields[{index}].role"] = allowed.Count == 0
                    ? [$"Kafelek typu '{input.Display}' nie przyjmuje ról pól."]
                    : [$"Rola pola w kafelku typu '{input.Display}' musi mieć jedną z wartości: {string.Join(", ", allowed)}."];
            }
        }

        var required = input.Display switch
        {
            TileDisplays.List => TileFieldRoles.Title,
            TileDisplays.Summary => TileFieldRoles.Primary,
            _ => null
        };
        if (required is not null && input.Fields.Count > 0 && input.Fields.All(field => field.Role != required))
        {
            errors.TryAdd("fields", [$"Kafelek typu '{input.Display}' wymaga co najmniej jednego pola z rolą '{required}'."]);
        }
    }

    private static void ValidateItems(TileInput input, HashSet<string> sources, Dictionary<string, string[]> errors)
    {
        if (!TileDisplays.HasItems(input.Display))
        {
            if (input.Items is not null)
            {
                errors["items"] = [$"Kafelek typu '{input.Display}' nie ma elementów."];
            }

            return;
        }

        if (input.Items is null)
        {
            errors["items"] = [$"Kafelek typu '{input.Display}' wymaga elementów (items)."];
            return;
        }

        if (string.IsNullOrWhiteSpace(input.Items.Source) || !sources.Contains(input.Items.Source))
        {
            errors["items.source"] = [$"Źródło elementów '{input.Items.Source}' nie istnieje w kafelku."];
        }

        if (!JsonPathParser.TryParse(input.Items.Path, out var path) || !path.HasWildcard)
        {
            errors["items.path"] = ["Ścieżka elementów jest wymagana i musi być poprawną ścieżką JSONPath z wildcardem."];
        }

        if (!input.Fields.Any(field => TileLabels.AreEqual(field.Label, input.Items.Key) && field.Source == input.Items.Source))
        {
            errors["items.key"] = [$"Klucz elementu '{input.Items.Key}' musi wskazywać etykietę pola ze źródła elementów."];
        }

        for (var index = 0; index < input.Fields.Count; index++)
        {
            var field = input.Fields[index];
            if (field.Source != input.Items.Source && !field.Hidden && sources.Contains(field.Source ?? string.Empty))
            {
                errors.TryAdd(
                    $"fields[{index}].source",
                    ["Pole spoza źródła elementów musi być ukryte i służy tylko jako źródło wiązania."]);
            }
        }
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

            if (!TileSourceKinds.All.Contains(source.Kind))
            {
                errors[$"{key}.kind"] = [$"Rodzaj źródła musi mieć jedną z wartości: {string.Join(", ", TileSourceKinds.All)}."];
            }
            else if (source.Kind == TileSourceKinds.Connection && source.ConnectionId is null)
            {
                errors[$"{key}.connectionId"] = ["Połączenie źródła jest wymagane."];
            }
            else if (source.Kind == TileSourceKinds.Input && source.ConnectionId is not null)
            {
                errors[$"{key}.connectionId"] = ["Wejście danych nie wskazuje połączenia."];
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
