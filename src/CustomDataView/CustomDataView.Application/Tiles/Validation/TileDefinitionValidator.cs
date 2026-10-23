using CustomDataView.Application.Tiles.Inputs;
using CustomDataView.Application.Tiles.Preview;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Services;

namespace CustomDataView.Application.Tiles.Validation;

public static class TileDefinitionValidator
{
    private const int MaxTextLength = 100;
    private const int MaxCssClassLength = 200;

    public static IDictionary<string, string[]> Validate(TileInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (!IsValidText(input.Name))
        {
            errors["name"] = [$"Nazwa kafelka jest wymagana i może mieć najwyżej {MaxTextLength} znaków."];
        }

        var displayIsValid = TileDisplays.All.Contains(input.Display);
        if (!displayIsValid)
        {
            errors["display"] = [$"Typ wyświetlania jest wymagany i musi mieć jedną z wartości: {string.Join(", ", TileDisplays.All)}."];
        }

        if (input.Fields.Count == 0 && !input.IsTabs)
        {
            errors["fields"] = ["Kafelek musi mieć co najmniej jedno pole."];
        }

        var labels = new HashSet<string>();
        for (var index = 0; index < input.Fields.Count; index++)
        {
            var label = input.Fields[index].Label;
            if (!IsValidText(label))
            {
                errors[$"fields[{index}].label"] = [$"Etykieta pola jest wymagana i może mieć najwyżej {MaxTextLength} znaków."];
            }
            else if (!labels.Add(TileLabels.Normalize(label)))
            {
                errors[$"fields[{index}].label"] = ["Etykieta pola musi być unikalna w kafelku."];
            }
        }

        if ((input.IsForm || input.IsTabs) && input.Actions.Count > 0)
        {
            errors["actions"] = [$"Kafelek typu '{input.Display}' nie ma akcji."];
        }

        if (input.IsForm)
        {
            ValidateForm(input, errors);
            return errors;
        }

        if (input.IsTabs)
        {
            ValidateTabs(input, errors);
            ValidateChildren(input, [], errors);
            TileBindingsValidator.ValidateShape(input, errors);
            return errors;
        }

        var sources = ValidateSources(input.Sources, errors);
        for (var index = 0; index < input.Fields.Count; index++)
        {
            ValidateField(input.Fields[index], $"fields[{index}]", sources, errors);
        }

        if (displayIsValid)
        {
            ValidateRoles(input, errors);
            ValidateItems(input, sources, errors);
            ValidatePagination(input, errors);
        }

        ValidateChildren(input, sources, errors);
        ValidateActions(input, errors);
        TileBindingsValidator.ValidateShape(input, errors);
        return errors;
    }

    private static void ValidateForm(TileInput input, Dictionary<string, string[]> errors)
    {
        if (input.Sources.Count > 0)
        {
            errors["sources"] = ["Formularz nie ma źródeł danych."];
        }

        if (input.Items is not null)
        {
            errors["items"] = ["Formularz nie ma elementów."];
        }

        if (input.Bindings.Count > 0)
        {
            errors["bindings"] = ["Formularz nie ma wiązań."];
        }

        if (input.Children.Count > 0)
        {
            errors["children"] = ["Formularz nie ma dzieci."];
        }

        for (var index = 0; index < input.Fields.Count; index++)
        {
            var field = input.Fields[index];
            var key = $"fields[{index}]";
            if (field.Source is not null || field.Path is not null || field.Transformations.Count > 0 || field.Hidden || field.Role is not null)
            {
                errors[key] = ["Pole formularza ma tylko etykietę, typ, wymaganie i wartość domyślną."];
            }

            if (!TileFormFieldTypes.All.Contains(field.Type))
            {
                errors[$"{key}.type"] = [$"Typ pola formularza jest wymagany i musi mieć jedną z wartości: {string.Join(", ", TileFormFieldTypes.All)}."];
            }
            else if (field.Default is not null && !TileFormFieldTypes.Matches(field.Type, field.Default))
            {
                errors[$"{key}.default"] = [$"Wartość domyślna musi być zgodna z typem '{field.Type}'."];
            }
        }
    }

    private static void ValidateTabs(TileInput input, Dictionary<string, string[]> errors)
    {
        if (input.Fields.Count > 0)
        {
            errors["fields"] = ["Kafelek z zakładkami nie ma pól."];
        }

        if (input.Sources.Count > 0)
        {
            errors["sources"] = ["Kafelek z zakładkami nie ma źródeł danych."];
        }

        if (input.Items is not null)
        {
            errors["items"] = ["Kafelek z zakładkami nie ma elementów."];
        }

        if (input.Children.Count == 0)
        {
            errors["children"] = ["Kafelek z zakładkami musi mieć co najmniej jedną zakładkę."];
        }

        for (var index = 0; index < input.Children.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(input.Children[index].Label))
            {
                errors[$"children[{index}].label"] = ["Tytuł zakładki jest wymagany."];
            }
        }
    }

    private static void ValidateChildren(TileInput input, HashSet<string> sources, Dictionary<string, string[]> errors)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var positions = new HashSet<int>();
        for (var index = 0; index < input.Children.Count; index++)
        {
            var child = input.Children[index];
            var key = $"children[{index}]";
            if (!TileNodeKeys.IsValid(child.Key))
            {
                errors[$"{key}.key"] =
                [
                    $"Klucz dziecka jest wymagany, może mieć najwyżej {TileNodeKeys.MaxLength} znaków, nie może zawierać znaku '/' ani być słowem '{TileNodeKeys.Parent}'."
                ];
            }
            else if (sources.Contains(child.Key) || !keys.Add(child.Key))
            {
                errors[$"{key}.key"] = [$"Klucz '{child.Key}' występuje w kafelku więcej niż raz (źródła i dzieci mają wspólne klucze)."];
            }

            if (child.TileId is null)
            {
                errors[$"{key}.tileId"] = ["Kafelek dziecka jest wymagany."];
            }

            if (child.Position is null or < 0)
            {
                errors[$"{key}.position"] = ["Pozycja dziecka jest wymagana i nie może być mniejsza niż 0."];
            }
            else if (!positions.Add(child.Position.Value))
            {
                errors[$"{key}.position"] = [$"Pozycja {child.Position} występuje w kafelku więcej niż raz."];
            }

            if (child.Scope is not null && !TileChildScopes.All.Contains(child.Scope))
            {
                errors[$"{key}.scope"] = [$"Zakres dziecka musi mieć jedną z wartości: {string.Join(", ", TileChildScopes.All)}."];
            }
            else if (child.Scope == TileChildScopes.Item && input.Items is null)
            {
                errors[$"{key}.scope"] = ["Zakres 'item' jest dozwolony tylko u rodzica z elementami."];
            }

            if (child.Load is not null && !TileChildLoads.All.Contains(child.Load))
            {
                errors[$"{key}.load"] = [$"Ładowanie dziecka musi mieć jedną z wartości: {string.Join(", ", TileChildLoads.All)}."];
            }

            if (child.Label?.Trim().Length > MaxTextLength)
            {
                errors[$"{key}.label"] = [$"Tytuł osadzenia może mieć najwyżej {MaxTextLength} znaków."];
            }

            if (child.CssClass?.Trim().Length > MaxCssClassLength)
            {
                errors[$"{key}.cssClass"] = [$"Klasa CSS może mieć najwyżej {MaxCssClassLength} znaków."];
            }
        }
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

        if (!JsonPathParser.TryParse(input.Items.Path, out var path) || !path.IsCollection)
        {
            errors["items.path"] = ["Ścieżka elementów jest wymagana i musi być poprawną ścieżką JSONPath z wildcardem albo filtrem."];
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

    private static void ValidateActions(TileInput input, Dictionary<string, string[]> errors)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < input.Actions.Count; index++)
        {
            var action = input.Actions[index];
            var key = $"actions[{index}]";
            if (!TileNodeKeys.IsValid(action.Key))
            {
                errors[$"{key}.key"] = [$"Klucz akcji jest wymagany, może mieć najwyżej {TileNodeKeys.MaxLength} znaków i nie może zawierać znaku '/'."];
            }
            else if (!keys.Add(action.Key))
            {
                errors[$"{key}.key"] = [$"Klucz akcji '{action.Key}' występuje w kafelku więcej niż raz."];
            }

            if (!IsValidText(action.Label))
            {
                errors[$"{key}.label"] = [$"Etykieta akcji jest wymagana i może mieć najwyżej {MaxTextLength} znaków."];
            }

            if (action.Type is not null && !TileActionTypes.All.Contains(action.Type))
            {
                errors[$"{key}.type"] = [$"Typ akcji musi mieć jedną z wartości: {string.Join(", ", TileActionTypes.All)}."];
            }

            if (action.TileId is null)
            {
                errors[$"{key}.tileId"] = ["Kafelek docelowy akcji jest wymagany."];
            }

            var scope = action.Scope ?? (input.Items is null ? TileActionScopes.Tile : TileActionScopes.Item);
            if (!TileActionScopes.All.Contains(scope))
            {
                errors[$"{key}.scope"] = [$"Zakres akcji musi mieć jedną z wartości: {string.Join(", ", TileActionScopes.All)}."];
                continue;
            }

            if (scope == TileActionScopes.Item && input.Items is null)
            {
                errors[$"{key}.scope"] = ["Zakres 'item' jest dozwolony tylko w kafelku z elementami."];
                continue;
            }

            var targets = new HashSet<string>(StringComparer.Ordinal);
            for (var position = 0; position < action.Map.Count; position++)
            {
                var mapping = action.Map[position];
                var mappingKey = $"{key}.map[{position}]";
                var itemField = scope == TileActionScopes.Item;
                if (!input.Fields.Any(field => TileLabels.AreEqual(field.Label, mapping.Field)
                        && (input.Items is null || (field.Source == input.Items.Source) == itemField)))
                {
                    errors[$"{mappingKey}.field"] = itemField
                        ? [$"Pole '{mapping.Field}' nie jest polem elementu."]
                        : [$"Pole '{mapping.Field}' nie istnieje w kafelku poza polami elementów."];
                }

                if (string.IsNullOrWhiteSpace(mapping.Parameter) == string.IsNullOrWhiteSpace(mapping.Input))
                {
                    errors[mappingKey] = ["Mapowanie wskazuje dokładnie jedno z: parametr albo wejście danych."];
                }
                else if (!targets.Add(mapping.Parameter ?? mapping.Input))
                {
                    errors[$"{mappingKey}.{(mapping.Parameter is null ? "input" : "parameter")}"] =
                        [$"Wejście '{mapping.Parameter ?? mapping.Input}' ma już wartość w tej akcji."];
                }
            }
        }
    }

    private static void ValidatePagination(TileInput input, Dictionary<string, string[]> errors)
    {
        var pagination = input.Pagination;
        if (input.Display != TileDisplays.PaginationTable)
        {
            if (pagination is not null)
            {
                errors["pagination"] = [$"Stronicowanie jest dostępne tylko w kafelku typu '{TileDisplays.PaginationTable}'."];
            }

            return;
        }

        if (pagination is null)
        {
            errors["pagination"] = [$"Kafelek typu '{TileDisplays.PaginationTable}' wymaga stronicowania (pagination)."];
            return;
        }

        if (pagination.PageSize is not (>= 1 and <= TilePages.MaxPageSize))
        {
            errors["pagination.pageSize"] = [$"Rozmiar strony jest wymagany i musi wynosić od 1 do {TilePages.MaxPageSize}."];
        }

        var definition = input.ToDefinition().Pagination;
        if (definition.PageNumberParameter.Length > TileNodeKeys.MaxLength || definition.PageSizeParameter.Length > TileNodeKeys.MaxLength)
        {
            errors["pagination"] = [$"Nazwy parametrów stronicowania mogą mieć najwyżej {TileNodeKeys.MaxLength} znaków."];
        }

        if (definition.PageNumberParameter == definition.PageSizeParameter)
        {
            errors["pagination.pageSizeParameter"] = ["Parametry numeru i rozmiaru strony muszą mieć różne nazwy."];
        }

        if (definition.TotalPath is not null && (!JsonPathParser.TryParse(definition.TotalPath, out var path) || path.IsCollection))
        {
            errors["pagination.totalPath"] = ["Ścieżka łącznej liczby elementów musi być poprawną ścieżką JSONPath bez wildcardu i filtra."];
        }

        if (input.Items is not null && input.Sources.Any(source => source.Key == input.Items.Source && source.Kind != TileSourceKinds.Connection))
        {
            errors["pagination"] = ["Źródłem elementów tabeli ze stronicowaniem musi być połączenie."];
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
            if (!TransformationParameterValues.IsStorable(field.Transformations[position].Parameter))
            {
                errors[$"{key}.transformations[{position}].parameter"] =
                    ["Wartość parametru transformacji musi być tekstem lub liczbą."];
            }
        }
    }

    private static bool IsValidText(string text) => !string.IsNullOrWhiteSpace(text) && text.Length <= MaxTextLength;
}
