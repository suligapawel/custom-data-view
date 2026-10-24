using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Services.Combinations;
using CustomDataView.Domain.Services.Transformations;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services;

public sealed class TileReferencesPolicy(
    TileTree tree,
    Func<Guid, ConnectionDefinition> connectionOf,
    IReadOnlySet<string> transformations,
    CombinationService combinations)
{
    public IReadOnlyDictionary<string, string[]> Check(TileDefinition definition, Guid? tileId = null)
    {
        var violations = new DefinitionViolations();
        for (var index = 0; index < definition.Sources.Count; index++)
        {
            if (definition.Sources[index].ConnectionId is { } connectionId && connectionOf(connectionId) is null)
            {
                violations.Set($"sources[{index}].connectionId", $"Połączenie '{connectionId}' nie istnieje.");
            }
        }

        for (var index = 0; index < definition.Fields.Count; index++)
        {
            var steps = definition.Fields[index].Transformations;
            for (var position = 0; position < steps.Count; position++)
            {
                if (!transformations.Contains(steps[position].Key))
                {
                    violations.Set($"fields[{index}].transformations[{position}].key", $"Transformacja '{steps[position].Key}' nie istnieje w katalogu.");
                }
                else if (steps[position].Key == MapperTransformation.TransformationKey)
                {
                    CheckDictionaryReference(steps[position], $"fields[{index}].transformations[{position}].parameter", violations);
                }
            }
        }

        for (var index = 0; index < definition.Fields.Count; index++)
        {
            if (definition.Fields[index].IsCombined)
            {
                CheckCombination(definition, definition.Fields[index], $"fields[{index}]", violations);
            }
        }

        CheckPagination(definition, violations);
        CheckActions(definition, violations);
        var childrenAreValid = CheckChildren(definition, tileId, violations);
        if (violations.Count == 0 && childrenAreValid)
        {
            CheckBindings(definition, violations);
        }

        return violations.ToDictionary();
    }

    private TileContract ContractOf(TileDefinition tile) => tile.Contract(connectionOf);

    private static void CheckDictionaryReference(TransformationStep step, string path, DefinitionViolations violations)
    {
        if (!DictionaryReference.TryParse(step.Parameter, out _))
        {
            violations.Set(path, $"Parametr transformacji '{MapperTransformation.TransformationKey}' ma format '<tileId>:<etykieta pola wartości>'.");
        }
    }

    private void CheckCombination(TileDefinition definition, TileField field, string path, DefinitionViolations violations)
    {
        var combination = combinations.Find(field.Combination.Key);
        if (combination is null)
        {
            violations.Set($"{path}.combine.key", $"Kombinator '{field.Combination.Key}' nie istnieje w katalogu.");
            return;
        }

        foreach (var error in combination.Validate(field.Combination.Parameter, field.Combination.Fields))
        {
            violations.TryAdd($"{path}.combine.{error.Target}", error.Message);
        }

        var inputErrors = combination.Inputs(field.Combination.Parameter, field.Combination.Fields)
            .Select(label => InputError(definition, field, combination, label))
            .Where(error => error is not null)
            .ToArray();
        if (inputErrors.Length > 0)
        {
            violations.Set($"{path}.combine", inputErrors);
        }

        if (!combination.AcceptsTransformations && field.Transformations.Count > 0)
        {
            violations.Set($"{path}.transformations", $"Pole z kombinatorem '{combination.Key}' nie przyjmuje transformacji.");
        }
    }

    private static string InputError(TileDefinition definition, TileField field, ICombination combination, string label)
    {
        if (TileLabels.AreEqual(label, field.Label))
        {
            return $"Pole '{label}' nie może składać się z samego siebie.";
        }

        var input = definition.Field(label);
        if (input is null)
        {
            return $"Pole '{label}' nie istnieje w kafelku.";
        }

        if (input.IsCombined)
        {
            return $"Pole '{label}' jest polem złożonym i nie może być wejściem kombinatora.";
        }

        if (definition.Items is not null && input.Source != definition.Items.Source)
        {
            return $"Pole '{label}' nie pochodzi ze źródła elementów.";
        }

        return !combination.AcceptsCollections && input.IsCollection
            ? $"Pole '{label}' zwraca listę, której kombinator '{combination.Key}' nie przyjmuje."
            : null;
    }

    private void CheckPagination(TileDefinition definition, DefinitionViolations violations)
    {
        if (definition.Pagination is not { } pagination
            || definition.Source(definition.Items?.Source)?.ConnectionId is not { } connectionId
            || connectionOf(connectionId) is not { } connection)
        {
            return;
        }

        var declared = connection.DeclaredNames;
        if (!declared.Contains(pagination.PageNumberParameter))
        {
            violations.Set(
                "pagination.pageNumberParameter",
                $"Parametr '{pagination.PageNumberParameter}' nie jest zadeklarowany w połączeniu źródła elementów.");
        }

        if (!declared.Contains(pagination.PageSizeParameter))
        {
            violations.Set(
                "pagination.pageSizeParameter",
                $"Parametr '{pagination.PageSizeParameter}' nie jest zadeklarowany w połączeniu źródła elementów.");
        }
    }

    private void CheckActions(TileDefinition definition, DefinitionViolations violations)
    {
        for (var index = 0; index < definition.Actions.Count; index++)
        {
            var action = definition.Actions[index];
            if (tree.Tile(action.TileId) is not { } target)
            {
                violations.Set($"actions[{index}].tileId", $"Kafelek '{action.TileId}' nie istnieje.");
                continue;
            }

            var contract = ContractOf(target);
            for (var position = 0; position < action.Map.Count; position++)
            {
                var mapping = action.Map[position];
                if (mapping.Parameter is not null && !contract.DeclaresParameter(mapping.Parameter))
                {
                    violations.Set($"actions[{index}].map[{position}].parameter", $"Parametr '{mapping.Parameter}' nie jest wejściem kafelka docelowego.");
                }

                if (mapping.Input is not null && !contract.DeclaresInput(mapping.Input))
                {
                    violations.Set($"actions[{index}].map[{position}].input", $"Wejście danych '{mapping.Input}' nie jest wejściem kafelka docelowego.");
                }
            }
        }
    }

    private bool CheckChildren(TileDefinition definition, Guid? tileId, DefinitionViolations violations)
    {
        var valid = true;
        for (var index = 0; index < definition.Children.Count; index++)
        {
            var child = definition.Children[index];
            var tile = tree.Tile(child.TileId);
            if (tile is null)
            {
                violations.Set($"children[{index}].tileId", $"Kafelek '{child.TileId}' nie istnieje.");
                valid = false;
            }
            else if (tile.IsForm && child.Scope != TileChildScopes.Tile)
            {
                violations.Set($"children[{index}].scope", "Formularz może być osadzony tylko z zakresem 'tile'.");
            }
            else if (tileId is { } id && (child.TileId == id || tree.Embeds(tile, id)))
            {
                violations.Set($"children[{index}].tileId", "Osadzenie tworzy cykl zawierania kafelków.");
                valid = false;
            }
        }

        return valid;
    }

    private void CheckBindings(TileDefinition definition, DefinitionViolations violations)
    {
        var targets = new HashSet<(string, string, string)>();
        for (var index = 0; index < definition.Bindings.Count; index++)
        {
            var binding = definition.Bindings[index];
            var path = $"bindings[{index}]";
            var toPath = binding.ToSegments;
            var fromPath = binding.FromSegments;
            var fromSource = fromPath.Length == 1 ? definition.Source(binding.From) : null;
            var fromChild = definition.Child(fromPath[0]);
            if (!binding.IsFromParent && fromSource is null && fromChild is null)
            {
                violations.Set($"{path}.from", $"Węzeł '{binding.From}' nie istnieje w kafelku.");
                continue;
            }

            var fromTile = fromChild is null ? null : tree.SourceTile(fromChild, fromPath);
            if (fromChild is not null && fromTile is null)
            {
                violations.Set($"{path}.from", $"Ścieżka osadzeń '{binding.From}' nie istnieje albo prowadzi przez osadzenie o zakresie 'item'.");
                continue;
            }

            var toSource = definition.Source(toPath[0]);
            var toChild = definition.Child(toPath[0]);
            if (toSource is null && toChild is null)
            {
                violations.Set($"{path}.to", $"Węzeł '{toPath[0]}' nie istnieje w kafelku.");
                continue;
            }

            if (!IsAllowedPair(binding.IsFromParent, fromSource, fromChild, toSource, toChild, toPath.Length))
            {
                violations.Set($"{path}.to", $"Wiązanie '{binding.From}' → '{binding.To}' łączy niedozwoloną parę węzłów.");
                continue;
            }

            var target = toSource is null ? tree.Target(toChild, toPath) : null;
            if (toSource is null && target is null)
            {
                violations.Set($"{path}.to", $"Ścieżka osadzeń '{binding.To}' nie istnieje.");
                continue;
            }

            for (var position = 0; position < binding.Map.Count; position++)
            {
                var mapping = binding.Map[position];
                var mappingPath = $"{path}.map[{position}]";
                CheckMappingField(definition, binding, mapping, fromSource, fromTile, mappingPath, violations);
                CheckMappingScope(definition, binding, mapping, fromSource, fromTile, toChild, mappingPath, violations);
                if (toSource is not null)
                {
                    CheckSourceTarget(toSource, mapping, mappingPath, violations);
                }
                else
                {
                    CheckTileTarget(target, toPath, mapping, mappingPath, violations);
                }

                if (!targets.Add((binding.To, mapping.TargetKind, mapping.Target)))
                {
                    violations.Set(
                        $"{mappingPath}.{mapping.TargetKind}",
                        $"Wejście '{mapping.Target}' węzła '{binding.To}' ma już źródło w innym mapowaniu.");
                }
            }
        }

        var order = BindingGraph.Order(definition.NodeKeys, definition.BindingEdges(combinations));
        if (order.Cycle.Count > 0)
        {
            violations.TryAdd("bindings", $"Wiązania tworzą cykl między węzłami: {string.Join(", ", order.Cycle)}.");
        }
    }

    private static bool IsAllowedPair(
        bool fromParent,
        TileSource fromSource,
        TileChild fromChild,
        TileSource toSource,
        TileChild toChild,
        int toPathLength)
    {
        if (fromChild?.Scope == TileChildScopes.Item && toChild?.Scope != TileChildScopes.Item)
        {
            return false;
        }

        if (toSource is not null)
        {
            return toPathLength == 1
                && toSource.Kind == TileSourceKinds.Connection
                && (fromSource?.Kind == TileSourceKinds.Connection || fromChild?.Scope == TileChildScopes.Tile);
        }

        return fromParent || fromChild is not null;
    }

    private void CheckMappingField(
        TileDefinition definition,
        TileBinding binding,
        TileBindingMapping mapping,
        TileSource fromSource,
        TileDefinition fromTile,
        string path,
        DefinitionViolations violations)
    {
        var exists = fromTile is not null
            ? ContractOf(fromTile).Outputs.Fields.Any(label => TileLabels.AreEqual(label, mapping.Field))
            : definition.Fields.Any(field => TileLabels.AreEqual(field.Label, mapping.Field) && (fromSource is null || field.Source == fromSource.Key));
        if (!exists)
        {
            violations.Set($"{path}.field", $"Pole '{mapping.Field}' nie istnieje w węźle '{binding.From}'.");
        }
    }

    private static void CheckMappingScope(
        TileDefinition definition,
        TileBinding binding,
        TileBindingMapping mapping,
        TileSource fromSource,
        TileDefinition fromTile,
        TileChild toChild,
        string path,
        DefinitionViolations violations)
    {
        var fromItems = fromSource is not null
            ? definition.Items?.Source == fromSource.Key
            : fromTile is not null
                ? fromTile.Items is not null
                : definition.Items is not null && definition.Fields.Any(field =>
                    TileLabels.AreEqual(field.Label, mapping.Field) && definition.IsItemField(field));
        if (fromSource is not null && fromItems)
        {
            violations.Set($"{path}.field", "Pole elementu nie może zasilać wiązania źródło → źródło.");
            return;
        }

        if (mapping.Scope is null)
        {
            return;
        }

        if (!fromItems)
        {
            violations.Set($"{path}.scope", "Zakres jest dozwolony tylko dla pola węzła z elementami.");
        }
        else if (mapping.Scope == BindingScopes.Item && (!binding.IsFromParent || toChild?.Scope != TileChildScopes.Item))
        {
            violations.Set($"{path}.scope", "Zakres 'item' jest dozwolony tylko dla pola rodzica i dziecka o zakresie 'item'.");
        }
    }

    private void CheckSourceTarget(TileSource target, TileBindingMapping mapping, string path, DefinitionViolations violations)
    {
        if (mapping.Input is not null)
        {
            violations.Set($"{path}.input", "Źródło typu 'connection' nie ma wejść danych.");
            return;
        }

        if (connectionOf(target.ConnectionId.Value) is { } connection && !connection.DeclaredNames.Contains(mapping.Parameter))
        {
            violations.Set($"{path}.parameter", $"Parametr '{mapping.Parameter}' nie jest zadeklarowany w połączeniu źródła '{target.Key}'.");
        }
    }

    private void CheckTileTarget(TileTreeTarget target, string[] toPath, TileBindingMapping mapping, string path, DefinitionViolations violations)
    {
        var address = string.Join(TileNodeKeys.PathSeparator, toPath);
        var contract = ContractOf(target.Tile);
        if (mapping.Parameter is not null && !contract.DeclaresParameter(mapping.Parameter))
        {
            violations.Set($"{path}.parameter", $"Parametr '{mapping.Parameter}' nie jest wejściem kafelka '{address}'.");
        }

        if (mapping.Input is not null && !contract.DeclaresInput(mapping.Input))
        {
            violations.Set($"{path}.input", $"Wejście danych '{mapping.Input}' nie jest wejściem kafelka '{address}'.");
        }

        for (var index = 0; index < target.Intermediates.Count; index++)
        {
            var rest = string.Join(TileNodeKeys.PathSeparator, toPath.Skip(index + 1));
            if (target.Intermediates[index].BoundInputs(rest).Contains(mapping.Target))
            {
                violations.Set(
                    $"{path}.{mapping.TargetKind}",
                    $"Wejście '{mapping.Target}' kafelka '{address}' jest już związane przez kafelek '{string.Join(TileNodeKeys.PathSeparator, toPath.Take(index + 1))}'.");
                break;
            }
        }
    }
}
