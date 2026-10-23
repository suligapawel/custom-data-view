using CustomDataView.Application.Tiles.Contracts;
using CustomDataView.Application.Tiles.Inputs;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Services;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Validation;

public static class TileBindingsValidator
{
    public static void ValidateShape(TileInput input, Dictionary<string, string[]> errors)
    {
        var pairs = new HashSet<(string, string)>();
        for (var index = 0; index < input.Bindings.Count; index++)
        {
            var binding = input.Bindings[index];
            var key = $"bindings[{index}]";
            if (string.IsNullOrWhiteSpace(binding.From))
            {
                errors[$"{key}.from"] = ["Węzeł źródłowy wiązania jest wymagany."];
            }

            if (string.IsNullOrWhiteSpace(binding.To))
            {
                errors[$"{key}.to"] = ["Węzeł docelowy wiązania jest wymagany."];
            }
            else if (binding.To.Split(TileNodeKeys.PathSeparator)[0] == binding.From?.Split(TileNodeKeys.PathSeparator)[0])
            {
                errors[$"{key}.to"] = ["Węzeł docelowy musi być różny od źródłowego."];
            }
            else if (!pairs.Add((binding.From, binding.To)))
            {
                errors[key] = [$"Wiązanie '{binding.From}' → '{binding.To}' występuje w kafelku więcej niż raz."];
            }

            if (binding.Map.Count == 0)
            {
                errors[$"{key}.map"] = ["Wiązanie musi mieć co najmniej jedno mapowanie."];
            }

            for (var position = 0; position < binding.Map.Count; position++)
            {
                var mapping = binding.Map[position];
                var mappingKey = $"{key}.map[{position}]";
                if (string.IsNullOrWhiteSpace(mapping.Field))
                {
                    errors[$"{mappingKey}.field"] = ["Pole źródłowe mapowania jest wymagane."];
                }

                if (string.IsNullOrWhiteSpace(mapping.Parameter) == string.IsNullOrWhiteSpace(mapping.Input))
                {
                    errors[mappingKey] = ["Mapowanie wskazuje dokładnie jedno z: parametr albo wejście danych."];
                }

                if (mapping.Scope is not null && !BindingScopes.Values.Contains(mapping.Scope))
                {
                    errors[$"{mappingKey}.scope"] = [$"Zakres mapowania musi mieć jedną z wartości: {string.Join(", ", BindingScopes.Values)}."];
                }
            }
        }
    }

    public static void Validate(
        TileDefinition definition,
        Func<Guid, TileDefinition> tileOf,
        Func<Guid, Connection> connectionOf,
        TileContracts contracts,
        CombinationService combinations,
        Dictionary<string, string[]> errors)
    {
        var sources = definition.Sources.ToDictionary(source => source.Key, StringComparer.Ordinal);
        var children = definition.Children.ToDictionary(child => child.Key, StringComparer.Ordinal);
        var targets = new HashSet<(string, string, string)>();
        for (var index = 0; index < definition.Bindings.Count; index++)
        {
            var binding = definition.Bindings[index];
            var key = $"bindings[{index}]";
            var segments = binding.To.Split(TileNodeKeys.PathSeparator);
            var fromSegments = binding.From.Split(TileNodeKeys.PathSeparator);
            var fromParent = binding.From == TileNodeKeys.Parent;
            var fromSource = fromSegments.Length == 1 ? sources.GetValueOrDefault(binding.From) : null;
            var fromChild = children.GetValueOrDefault(fromSegments[0]);
            if (!fromParent && fromSource is null && fromChild is null)
            {
                errors[$"{key}.from"] = [$"Węzeł '{binding.From}' nie istnieje w kafelku."];
                continue;
            }

            var fromTile = fromChild is null ? null : SourceTile(fromChild, fromSegments, tileOf);
            if (fromChild is not null && fromTile is null)
            {
                errors[$"{key}.from"] = [$"Ścieżka osadzeń '{binding.From}' nie istnieje albo prowadzi przez osadzenie o zakresie 'item'."];
                continue;
            }

            var toSource = sources.GetValueOrDefault(segments[0]);
            var toChild = children.GetValueOrDefault(segments[0]);
            if (toSource is null && toChild is null)
            {
                errors[$"{key}.to"] = [$"Węzeł '{segments[0]}' nie istnieje w kafelku."];
                continue;
            }

            if (!IsAllowedPair(fromParent, fromSource, fromChild, toSource, toChild, segments.Length))
            {
                errors[$"{key}.to"] = [$"Wiązanie '{binding.From}' → '{binding.To}' łączy niedozwoloną parę węzłów."];
                continue;
            }

            var target = toSource is null ? Target(toChild, segments, tileOf) : null;
            if (toSource is null && target is null)
            {
                errors[$"{key}.to"] = [$"Ścieżka osadzeń '{binding.To}' nie istnieje."];
                continue;
            }

            for (var position = 0; position < binding.Map.Count; position++)
            {
                var mapping = binding.Map[position];
                var mappingKey = $"{key}.map[{position}]";
                ValidateField(definition, binding, mapping, fromSource, fromTile, contracts, mappingKey, errors);
                ValidateScope(definition, binding, mapping, fromSource, fromTile, toChild, segments.Length, mappingKey, errors);
                if (toSource is not null)
                {
                    ValidateSourceTarget(toSource, mapping, connectionOf, mappingKey, errors);
                }
                else
                {
                    ValidateTileTarget(target.Value, segments, mapping, contracts, mappingKey, errors);
                }

                var name = mapping.Parameter ?? mapping.Input;
                if (!targets.Add((binding.To, mapping.Parameter is null ? "input" : "parameter", name)))
                {
                    errors[$"{mappingKey}.{(mapping.Parameter is null ? "input" : "parameter")}"] =
                        [$"Wejście '{name}' węzła '{binding.To}' ma już źródło w innym mapowaniu."];
                }
            }
        }

        var order = BindingGraph.Order(
            [.. sources.Keys, .. children.Keys],
            Edges(definition, combinations));
        if (order.Cycle.Count > 0)
        {
            errors.TryAdd("bindings", [$"Wiązania tworzą cykl między węzłami: {string.Join(", ", order.Cycle)}."]);
        }
    }

    public static IEnumerable<(string From, string To)> Edges(TileDefinition definition, CombinationService combinations)
        => definition.Bindings.SelectMany(binding => binding.Map.SelectMany(mapping =>
            Sources(definition, binding, mapping, combinations).Select(from => (From: from, To: binding.To.Split(TileNodeKeys.PathSeparator)[0]))));

    private static IEnumerable<string> Sources(
        TileDefinition definition,
        TileBinding binding,
        TileBindingMapping mapping,
        CombinationService combinations)
    {
        if (binding.From != TileNodeKeys.Parent)
        {
            return [binding.From.Split(TileNodeKeys.PathSeparator)[0]];
        }

        var field = definition.Fields.FirstOrDefault(candidate => TileLabels.AreEqual(candidate.Label, mapping.Field));
        return field is { IsCombined: true }
            ? combinations.Inputs(definition, field).Select(input => input.Source)
            : [field?.Source ?? binding.From];
    }

    private static bool IsAllowedPair(
        bool fromParent,
        TileSource fromSource,
        TileChild fromChild,
        TileSource toSource,
        TileChild toChild,
        int segments)
    {
        if (fromChild?.Scope == TileChildScopes.Item && toChild?.Scope != TileChildScopes.Item)
        {
            return false;
        }

        if (toSource is not null)
        {
            return segments == 1
                && toSource.Kind == TileSourceKinds.Connection
                && ((fromSource?.Kind == TileSourceKinds.Connection) || fromChild?.Scope == TileChildScopes.Tile);
        }

        return fromParent || fromChild is not null;
    }

    private static TileDefinition SourceTile(TileChild child, string[] segments, Func<Guid, TileDefinition> tileOf)
    {
        if (segments.Length > 1 && child.Scope != TileChildScopes.Tile)
        {
            return null;
        }

        var tile = tileOf(child.TileId);
        foreach (var segment in segments.Skip(1))
        {
            var descendant = tile?.Children.FirstOrDefault(candidate => candidate.Key == segment);
            if (descendant is null || descendant.Scope != TileChildScopes.Tile)
            {
                return null;
            }

            tile = tileOf(descendant.TileId);
        }

        return tile;
    }

    private static (TileChild Child, TileDefinition Tile, IReadOnlyList<TileDefinition> Intermediates)? Target(
        TileChild child,
        string[] segments,
        Func<Guid, TileDefinition> tileOf)
    {
        var tile = tileOf(child.TileId);
        var intermediates = new List<TileDefinition>();
        foreach (var segment in segments.Skip(1))
        {
            var descendant = tile?.Children.FirstOrDefault(candidate => candidate.Key == segment);
            if (descendant is null)
            {
                return null;
            }

            intermediates.Add(tile);
            child = descendant;
            tile = tileOf(descendant.TileId);
        }

        return tile is null ? null : (child, tile, intermediates);
    }

    private static void ValidateField(
        TileDefinition definition,
        TileBinding binding,
        TileBindingMapping mapping,
        TileSource fromSource,
        TileDefinition fromTile,
        TileContracts contracts,
        string key,
        Dictionary<string, string[]> errors)
    {
        var exists = fromTile is not null
            ? contracts.Of(fromTile).Outputs.Fields.Any(label => TileLabels.AreEqual(label, mapping.Field))
            : definition.Fields.Any(field => TileLabels.AreEqual(field.Label, mapping.Field)
                && (fromSource is null || field.Source == fromSource.Key));
        if (!exists)
        {
            errors[$"{key}.field"] = [$"Pole '{mapping.Field}' nie istnieje w węźle '{binding.From}'."];
        }
    }

    private static void ValidateScope(
        TileDefinition definition,
        TileBinding binding,
        TileBindingMapping mapping,
        TileSource fromSource,
        TileDefinition fromTile,
        TileChild toChild,
        int segments,
        string key,
        Dictionary<string, string[]> errors)
    {
        var fromItems = fromSource is not null
            ? definition.Items?.Source == fromSource.Key
            : fromTile is not null
                ? fromTile.Items is not null
                : definition.Items is not null && definition.Fields.Any(field =>
                    TileLabels.AreEqual(field.Label, mapping.Field) && definition.IsItemField(field));
        if (fromSource is not null && fromItems)
        {
            errors[$"{key}.field"] = ["Pole elementu nie może zasilać wiązania źródło → źródło."];
            return;
        }

        if (mapping.Scope is null)
        {
            return;
        }

        if (!fromItems)
        {
            errors[$"{key}.scope"] = ["Zakres jest dozwolony tylko dla pola węzła z elementami."];
        }
        else if (mapping.Scope == BindingScopes.Item
                 && (binding.From != TileNodeKeys.Parent || toChild?.Scope != TileChildScopes.Item))
        {
            errors[$"{key}.scope"] = ["Zakres 'item' jest dozwolony tylko dla pola rodzica i dziecka o zakresie 'item'."];
        }
    }

    private static void ValidateSourceTarget(
        TileSource target,
        TileBindingMapping mapping,
        Func<Guid, Connection> connectionOf,
        string key,
        Dictionary<string, string[]> errors)
    {
        if (mapping.Input is not null)
        {
            errors[$"{key}.input"] = ["Źródło typu 'connection' nie ma wejść danych."];
            return;
        }

        if (connectionOf(target.ConnectionId.Value) is { } connection
            && !TileConnectionParameters.Names(connection.Definition).Contains(mapping.Parameter))
        {
            errors[$"{key}.parameter"] = [$"Parametr '{mapping.Parameter}' nie jest zadeklarowany w połączeniu źródła '{target.Key}'."];
        }
    }

    private static void ValidateTileTarget(
        (TileChild Child, TileDefinition Tile, IReadOnlyList<TileDefinition> Intermediates) target,
        string[] segments,
        TileBindingMapping mapping,
        TileContracts contracts,
        string key,
        Dictionary<string, string[]> errors)
    {
        var contract = contracts.Of(target.Tile);
        if (mapping.Parameter is not null && contract.Parameters.All(parameter => parameter.Name != mapping.Parameter))
        {
            errors[$"{key}.parameter"] = [$"Parametr '{mapping.Parameter}' nie jest wejściem kafelka '{string.Join('/', segments)}'."];
        }

        if (mapping.Input is not null && !contract.Inputs.Contains(mapping.Input))
        {
            errors[$"{key}.input"] = [$"Wejście danych '{mapping.Input}' nie jest wejściem kafelka '{string.Join('/', segments)}'."];
        }

        var name = mapping.Parameter ?? mapping.Input;
        for (var index = 0; index < target.Intermediates.Count; index++)
        {
            var rest = string.Join(TileNodeKeys.PathSeparator, segments.Skip(index + 1));
            if (target.Intermediates[index].Bindings
                .Where(binding => binding.To == rest)
                .SelectMany(binding => binding.Map)
                .Any(bound => (bound.Parameter ?? bound.Input) == name))
            {
                errors[$"{key}.{(mapping.Parameter is null ? "input" : "parameter")}"] =
                    [$"Wejście '{name}' kafelka '{string.Join(TileNodeKeys.PathSeparator, segments)}' jest już związane przez kafelek '{string.Join(TileNodeKeys.PathSeparator, segments.Take(index + 1))}'."];
                break;
            }
        }
    }
}
