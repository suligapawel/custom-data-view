using CustomDataView.Application.Exceptions;
using CustomDataView.Application.Tiles.Inputs;
using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.Services.Transformations;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Validation;

public sealed class TileReferencesValidator(IConnectionRepository connections, IEnumerable<ITransformation> transformations)
{
    private readonly HashSet<string> catalog =
        transformations.Select(transformation => transformation.Key).ToHashSet(StringComparer.Ordinal);

    public void EnsureValid(TileDefinition definition)
    {
        var errors = Validate(definition);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    public IDictionary<string, string[]> Validate(TileInput input) => Validate(input.ToDefinition());

    public IDictionary<string, string[]> Validate(TileDefinition definition)
    {
        var errors = new Dictionary<string, string[]>();
        var existing = definition.Sources
            .Where(source => source.ConnectionId is not null)
            .Select(source => source.ConnectionId.Value)
            .Distinct()
            .ToDictionary(id => id, connections.Get);
        for (var index = 0; index < definition.Sources.Count; index++)
        {
            if (definition.Sources[index].ConnectionId is { } connectionId && existing[connectionId] is null)
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

        ValidateBoundParameters(definition, existing, errors);
        return errors;
    }

    private static void ValidateBoundParameters(
        TileDefinition definition,
        IReadOnlyDictionary<Guid, Connection> existing,
        Dictionary<string, string[]> errors)
    {
        for (var index = 0; index < definition.Bindings.Count; index++)
        {
            var binding = definition.Bindings[index];
            var target = definition.Sources.FirstOrDefault(source => source.Key == binding.To);
            if (target?.ConnectionId is not { } connectionId || existing.GetValueOrDefault(connectionId) is not { } connection)
            {
                continue;
            }

            var declared = TileConnectionParameters.Names(connection.Definition);
            for (var position = 0; position < binding.Map.Count; position++)
            {
                var parameter = binding.Map[position].Parameter;
                if (parameter is not null && !declared.Contains(parameter))
                {
                    errors[$"bindings[{index}].map[{position}].parameter"] =
                        [$"Parametr '{parameter}' nie jest zadeklarowany w połączeniu źródła '{binding.To}'."];
                }
            }
        }
    }
}
