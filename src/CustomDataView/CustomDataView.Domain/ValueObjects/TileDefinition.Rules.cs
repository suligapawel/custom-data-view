using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Services;

namespace CustomDataView.Domain.ValueObjects;

public sealed partial record TileDefinition
{
    public IReadOnlyDictionary<string, string[]> Check()
    {
        var violations = new DefinitionViolations();
        if (!TileTexts.IsValid(Name))
        {
            violations.Set("name", $"Nazwa kafelka jest wymagana i może mieć najwyżej {TileTexts.MaxLength} znaków.");
        }

        var displayIsValid = TileDisplays.All.Contains(Display);
        if (!displayIsValid)
        {
            violations.Set("display", $"Typ wyświetlania jest wymagany i musi mieć jedną z wartości: {string.Join(", ", TileDisplays.All)}.");
        }

        if (OutputLabels.Count == 0 && !IsTabs)
        {
            violations.Set("fields", "Kafelek musi mieć co najmniej jedno pole.");
        }

        CheckLabels(violations);
        if ((IsForm || IsTabs) && Actions.Count > 0)
        {
            violations.Set("actions", $"Kafelek typu '{Display}' nie ma akcji.");
        }

        if (IsForm)
        {
            CheckForm(violations);
            return violations.ToDictionary();
        }

        if (FormFields.Count > 0)
        {
            violations.TryAdd("fields", "Pola formularza są dostępne tylko w kafelku typu 'form'.");
        }

        if (IsTabs)
        {
            CheckTabs(violations);
            CheckPagination(violations);
            CheckChildren(new HashSet<string>(StringComparer.Ordinal), violations);
            CheckBindings(violations);
            return violations.ToDictionary();
        }

        var sources = CheckSources(violations);
        for (var index = 0; index < Fields.Count; index++)
        {
            Fields[index].Check($"fields[{index}]", sources, violations);
        }

        if (displayIsValid)
        {
            CheckRoles(violations);
            CheckItems(sources, violations);
            CheckPagination(violations);
        }

        CheckChildren(sources, violations);
        CheckActions(violations);
        CheckBindings(violations);
        return violations.ToDictionary();
    }

    private void CheckLabels(DefinitionViolations violations)
    {
        var labels = new HashSet<string>();
        var outputLabels = OutputLabels;
        for (var index = 0; index < outputLabels.Count; index++)
        {
            var label = outputLabels[index];
            if (!TileTexts.IsValid(label))
            {
                violations.Set($"fields[{index}].label", $"Etykieta pola jest wymagana i może mieć najwyżej {TileTexts.MaxLength} znaków.");
            }
            else if (!labels.Add(TileLabels.Normalize(label)))
            {
                violations.Set($"fields[{index}].label", "Etykieta pola musi być unikalna w kafelku.");
            }
        }
    }

    private void CheckForm(DefinitionViolations violations)
    {
        if (Fields.Count > 0)
        {
            violations.TryAdd("fields", "Formularz ma tylko pola formularza.");
        }

        if (Sources.Count > 0)
        {
            violations.Set("sources", "Formularz nie ma źródeł danych.");
        }

        if (Items is not null)
        {
            violations.Set("items", "Formularz nie ma elementów.");
        }

        if (Bindings.Count > 0)
        {
            violations.Set("bindings", "Formularz nie ma wiązań.");
        }

        if (Children.Count > 0)
        {
            violations.Set("children", "Formularz nie ma dzieci.");
        }

        CheckPagination(violations);
        for (var index = 0; index < FormFields.Count; index++)
        {
            FormFields[index].Check($"fields[{index}]", violations);
        }
    }

    private void CheckTabs(DefinitionViolations violations)
    {
        if (Fields.Count > 0)
        {
            violations.Set("fields", "Kafelek z zakładkami nie ma pól.");
        }

        if (Sources.Count > 0)
        {
            violations.Set("sources", "Kafelek z zakładkami nie ma źródeł danych.");
        }

        if (Items is not null)
        {
            violations.Set("items", "Kafelek z zakładkami nie ma elementów.");
        }

        if (Children.Count == 0)
        {
            violations.Set("children", "Kafelek z zakładkami musi mieć co najmniej jedną zakładkę.");
        }

        for (var index = 0; index < Children.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(Children[index].Label))
            {
                violations.Set($"children[{index}].label", "Tytuł zakładki jest wymagany.");
            }
        }
    }

    private HashSet<string> CheckSources(DefinitionViolations violations)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < Sources.Count; index++)
        {
            var source = Sources[index];
            source.Check($"sources[{index}]", violations);
            if (TileNodeKeys.IsValid(source.Key) && !keys.Add(source.Key))
            {
                violations.Set($"sources[{index}].key", $"Klucz źródła '{source.Key}' występuje w kafelku więcej niż raz.");
            }
        }

        return keys;
    }

    private void CheckRoles(DefinitionViolations violations)
    {
        var allowed = TileFieldRoles.For(Display);
        for (var index = 0; index < Fields.Count; index++)
        {
            var role = Fields[index].Role;
            if (!string.IsNullOrWhiteSpace(role) && !allowed.Contains(role))
            {
                violations.Set(
                    $"fields[{index}].role",
                    allowed.Count == 0
                        ? $"Kafelek typu '{Display}' nie przyjmuje ról pól."
                        : $"Rola pola w kafelku typu '{Display}' musi mieć jedną z wartości: {string.Join(", ", allowed)}.");
            }
        }

        var required = TileFieldRoles.RequiredFor(Display);
        if (required is not null && Fields.Count > 0 && Fields.All(field => field.Role != required))
        {
            violations.TryAdd("fields", $"Kafelek typu '{Display}' wymaga co najmniej jednego pola z rolą '{required}'.");
        }
    }

    private void CheckItems(HashSet<string> sources, DefinitionViolations violations)
    {
        if (!TileDisplays.HasItems(Display))
        {
            if (Items is not null)
            {
                violations.Set("items", $"Kafelek typu '{Display}' nie ma elementów.");
            }

            return;
        }

        if (Items is null)
        {
            violations.Set("items", $"Kafelek typu '{Display}' wymaga elementów (items).");
            return;
        }

        if (string.IsNullOrWhiteSpace(Items.Source) || !sources.Contains(Items.Source))
        {
            violations.Set("items.source", $"Źródło elementów '{Items.Source}' nie istnieje w kafelku.");
        }

        if (!JsonPathParser.TryParse(Items.Path, out var path) || !path.IsCollection)
        {
            violations.Set("items.path", "Ścieżka elementów jest wymagana i musi być poprawną ścieżką JSONPath z wildcardem albo filtrem.");
        }

        if (!Fields.Any(field => TileLabels.AreEqual(field.Label, Items.Key) && field.Source == Items.Source))
        {
            violations.Set("items.key", $"Klucz elementu '{Items.Key}' musi wskazywać etykietę pola ze źródła elementów.");
        }

        for (var index = 0; index < Fields.Count; index++)
        {
            var field = Fields[index];
            if (field.Source != Items.Source && !field.Hidden && sources.Contains(field.Source ?? string.Empty))
            {
                violations.TryAdd($"fields[{index}].source", "Pole spoza źródła elementów musi być ukryte i służy tylko jako źródło wiązania.");
            }
        }
    }

    private void CheckPagination(DefinitionViolations violations)
    {
        if (Display != TileDisplays.PaginationTable)
        {
            if (Pagination is not null)
            {
                violations.Set("pagination", $"Stronicowanie jest dostępne tylko w kafelku typu '{TileDisplays.PaginationTable}'.");
            }

            return;
        }

        if (Pagination is null)
        {
            violations.Set("pagination", $"Kafelek typu '{TileDisplays.PaginationTable}' wymaga stronicowania (pagination).");
            return;
        }

        Pagination.Check(violations);
        if (Items is not null && Sources.Any(source => source.Key == Items.Source && source.Kind != TileSourceKinds.Connection))
        {
            violations.Set("pagination", "Źródłem elementów tabeli ze stronicowaniem musi być połączenie.");
        }
    }

    private void CheckChildren(HashSet<string> sources, DefinitionViolations violations)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var positions = new HashSet<int>();
        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            var path = $"children[{index}]";
            child.Check(path, violations);
            if (TileNodeKeys.IsValid(child.Key) && (sources.Contains(child.Key) || !keys.Add(child.Key)))
            {
                violations.Set($"{path}.key", $"Klucz '{child.Key}' występuje w kafelku więcej niż raz (źródła i dzieci mają wspólne klucze).");
            }

            if (child.Position >= 0 && !positions.Add(child.Position))
            {
                violations.Set($"{path}.position", $"Pozycja {child.Position} występuje w kafelku więcej niż raz.");
            }

            if (child.Scope == TileChildScopes.Item && Items is null)
            {
                violations.Set($"{path}.scope", "Zakres 'item' jest dozwolony tylko u rodzica z elementami.");
            }
        }
    }

    private void CheckActions(DefinitionViolations violations)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < Actions.Count; index++)
        {
            var action = Actions[index];
            var path = $"actions[{index}]";
            action.Check(path, violations);
            if (TileNodeKeys.IsValid(action.Key) && !keys.Add(action.Key))
            {
                violations.Set($"{path}.key", $"Klucz akcji '{action.Key}' występuje w kafelku więcej niż raz.");
            }

            if (!TileActionScopes.All.Contains(action.Scope))
            {
                continue;
            }

            if (action.Scope == TileActionScopes.Item && Items is null)
            {
                violations.Set($"{path}.scope", "Zakres 'item' jest dozwolony tylko w kafelku z elementami.");
                continue;
            }

            CheckActionMappings(action, path, violations);
        }
    }

    private void CheckActionMappings(TileAction action, string path, DefinitionViolations violations)
    {
        var itemField = action.Scope == TileActionScopes.Item;
        var targets = new HashSet<string>(StringComparer.Ordinal);
        for (var position = 0; position < action.Map.Count; position++)
        {
            var mapping = action.Map[position];
            var mappingPath = $"{path}.map[{position}]";
            if (!Fields.Any(field => TileLabels.AreEqual(field.Label, mapping.Field) && (Items is null || IsItemField(field) == itemField)))
            {
                violations.Set(
                    $"{mappingPath}.field",
                    itemField
                        ? $"Pole '{mapping.Field}' nie jest polem elementu."
                        : $"Pole '{mapping.Field}' nie istnieje w kafelku poza polami elementów.");
            }

            if (!mapping.TargetsExactlyOne)
            {
                violations.Set(mappingPath, "Mapowanie wskazuje dokładnie jedno z: parametr albo wejście danych.");
            }
            else if (!targets.Add(mapping.Target))
            {
                violations.Set($"{mappingPath}.{mapping.TargetKind}", $"Wejście '{mapping.Target}' ma już wartość w tej akcji.");
            }
        }
    }

    private void CheckBindings(DefinitionViolations violations)
    {
        var pairs = new HashSet<(string, string)>();
        for (var index = 0; index < Bindings.Count; index++)
        {
            var binding = Bindings[index];
            var path = $"bindings[{index}]";
            binding.Check(path, violations);
            if (binding.HasDistinctEnds && !pairs.Add((binding.From, binding.To)))
            {
                violations.Set(path, $"Wiązanie '{binding.From}' → '{binding.To}' występuje w kafelku więcej niż raz.");
            }
        }
    }
}
