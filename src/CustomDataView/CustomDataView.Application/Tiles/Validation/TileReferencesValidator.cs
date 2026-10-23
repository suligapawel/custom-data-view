using CustomDataView.Application.Exceptions;
using CustomDataView.Application.Tiles.Inputs;
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

    public IDictionary<string, string[]> Validate(TileDefinition definition)
        => Validate(
            definition.Sources.Select(source => source.ConnectionId).ToList(),
            definition.Fields.Select(field => field.Transformations.Select(step => step.Key).ToList()).ToList());

    public IDictionary<string, string[]> Validate(TileInput input)
        => Validate(
            input.Sources.Select(source => source.ConnectionId).ToList(),
            input.Fields.Select(field => field.Transformations.Select(step => step.Key).ToList()).ToList());

    private Dictionary<string, string[]> Validate(
        IReadOnlyList<Guid?> sourceConnections,
        IReadOnlyList<List<string>> fieldTransformations)
    {
        var errors = new Dictionary<string, string[]>();
        var existing = sourceConnections
            .Where(id => id is not null)
            .Select(id => id.Value)
            .Distinct()
            .ToDictionary(id => id, id => connections.Get(id) is not null);
        for (var index = 0; index < sourceConnections.Count; index++)
        {
            if (sourceConnections[index] is { } connectionId && !existing[connectionId])
            {
                errors[$"sources[{index}].connectionId"] = [$"Połączenie '{connectionId}' nie istnieje."];
            }
        }

        for (var index = 0; index < fieldTransformations.Count; index++)
        {
            for (var position = 0; position < fieldTransformations[index].Count; position++)
            {
                var key = fieldTransformations[index][position];
                if (!catalog.Contains(key))
                {
                    errors[$"fields[{index}].transformations[{position}].key"] = [$"Transformacja '{key}' nie istnieje w katalogu."];
                }
            }
        }

        return errors;
    }
}
