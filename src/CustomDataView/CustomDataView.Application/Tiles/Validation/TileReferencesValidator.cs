using CustomDataView.Application.Exceptions;
using CustomDataView.Application.Tiles.Contracts;
using CustomDataView.Application.Tiles.Inputs;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.Services;
using CustomDataView.Domain.Services.Transformations;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Validation;

public sealed class TileReferencesValidator(
    IConnectionRepository connections,
    ITileRepository tiles,
    TileContracts contracts,
    IEnumerable<ITransformation> transformations,
    CombinationService combinations)
{
    private readonly HashSet<string> catalog =
        transformations.Select(transformation => transformation.Key).ToHashSet(StringComparer.Ordinal);

    public void EnsureValid(TileDefinition definition, Guid? tileId = null)
    {
        var errors = Validate(definition, tileId);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    public IDictionary<string, string[]> Validate(TileInput input) => Validate(input.ToDefinition());

    public IDictionary<string, string[]> Validate(TileDefinition definition, Guid? tileId = null)
    {
        var errors = new Dictionary<string, string[]>();
        var connectionCache = new Dictionary<Guid, Connection>();
        var tileCache = new Dictionary<Guid, TileDefinition>();
        Connection ConnectionOf(Guid id) => connectionCache.TryGetValue(id, out var cached) ? cached : connectionCache[id] = connections.Get(id);
        TileDefinition TileOf(Guid id) => tileCache.TryGetValue(id, out var cached) ? cached : tileCache[id] = tiles.Get(id)?.Definition;

        for (var index = 0; index < definition.Sources.Count; index++)
        {
            if (definition.Sources[index].ConnectionId is { } connectionId && ConnectionOf(connectionId) is null)
            {
                errors[$"sources[{index}].connectionId"] = [$"Połączenie '{connectionId}' nie istnieje."];
            }
        }

        for (var index = 0; index < definition.Fields.Count; index++)
        {
            var steps = definition.Fields[index].Transformations;
            for (var position = 0; position < steps.Count; position++)
            {
                if (!catalog.Contains(steps[position].Key))
                {
                    errors[$"fields[{index}].transformations[{position}].key"] =
                        [$"Transformacja '{steps[position].Key}' nie istnieje w katalogu."];
                }
            }
        }

        if (definition.Pagination is { } pagination
            && definition.Sources.FirstOrDefault(source => source.Key == definition.Items?.Source)?.ConnectionId is { } pagedConnectionId
            && ConnectionOf(pagedConnectionId) is { } pagedConnection)
        {
            var declared = TileConnectionParameters.Names(pagedConnection.Definition);
            if (!declared.Contains(pagination.PageNumberParameter))
            {
                errors["pagination.pageNumberParameter"] =
                    [$"Parametr '{pagination.PageNumberParameter}' nie jest zadeklarowany w połączeniu źródła elementów."];
            }

            if (!declared.Contains(pagination.PageSizeParameter))
            {
                errors["pagination.pageSizeParameter"] =
                    [$"Parametr '{pagination.PageSizeParameter}' nie jest zadeklarowany w połączeniu źródła elementów."];
            }
        }

        for (var index = 0; index < definition.Actions.Count; index++)
        {
            var action = definition.Actions[index];
            if (TileOf(action.TileId) is not { } target)
            {
                errors[$"actions[{index}].tileId"] = [$"Kafelek '{action.TileId}' nie istnieje."];
                continue;
            }

            var contract = contracts.Of(target);
            for (var position = 0; position < action.Map.Count; position++)
            {
                var mapping = action.Map[position];
                if (mapping.Parameter is not null && contract.Parameters.All(parameter => parameter.Name != mapping.Parameter))
                {
                    errors[$"actions[{index}].map[{position}].parameter"] = [$"Parametr '{mapping.Parameter}' nie jest wejściem kafelka docelowego."];
                }

                if (mapping.Input is not null && !contract.Inputs.Contains(mapping.Input))
                {
                    errors[$"actions[{index}].map[{position}].input"] = [$"Wejście danych '{mapping.Input}' nie jest wejściem kafelka docelowego."];
                }
            }
        }

        var childrenAreValid = ValidateChildren(definition, tileId, TileOf, errors);
        if (errors.Count == 0 && childrenAreValid)
        {
            TileBindingsValidator.Validate(definition, TileOf, ConnectionOf, contracts, combinations, errors);
        }

        return errors;
    }

    private bool ValidateChildren(
        TileDefinition definition,
        Guid? tileId,
        Func<Guid, TileDefinition> tileOf,
        Dictionary<string, string[]> errors)
    {
        var valid = true;
        for (var index = 0; index < definition.Children.Count; index++)
        {
            var child = definition.Children[index];
            var tile = tileOf(child.TileId);
            if (tile is null)
            {
                errors[$"children[{index}].tileId"] = [$"Kafelek '{child.TileId}' nie istnieje."];
                valid = false;
            }
            else if (tile.Display == TileDisplays.Form && child.Scope != TileChildScopes.Tile)
            {
                errors[$"children[{index}].scope"] = ["Formularz może być osadzony tylko z zakresem 'tile'."];
            }
            else if (tileId is { } id && (child.TileId == id || Contains(tile, id, tileOf, [])))
            {
                errors[$"children[{index}].tileId"] = ["Osadzenie tworzy cykl zawierania kafelków."];
                valid = false;
            }
        }

        return valid;
    }

    private static bool Contains(TileDefinition tile, Guid id, Func<Guid, TileDefinition> tileOf, HashSet<Guid> visited)
        => tile.Children.Any(child => child.TileId == id
            || (visited.Add(child.TileId) && tileOf(child.TileId) is { } descendant && Contains(descendant, id, tileOf, visited)));
}
