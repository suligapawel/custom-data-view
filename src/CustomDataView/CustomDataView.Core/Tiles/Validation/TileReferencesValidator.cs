using CustomDataView.Core.Connections.Persistence;
using CustomDataView.Core.Tiles.Definitions;
using CustomDataView.Core.Transformations.Abstractions;

namespace CustomDataView.Core.Tiles.Validation;

public sealed class TileReferencesValidator(IConnectionRepository connections, IEnumerable<ITransformation> transformations)
{
    private readonly HashSet<string> catalog =
        transformations.Select(transformation => transformation.Key).ToHashSet(StringComparer.Ordinal);

    public IDictionary<string, string[]> Validate(TileDefinition definition)
        => Validate(definition.Fields
            .Select(field => new FieldReferences(field.ConnectionId, field.Transformations.Select(step => step.Key).ToList()))
            .ToList());

    public IDictionary<string, string[]> Validate(TileInput input)
        => Validate(input.Fields
            .Select(field => new FieldReferences(field.ConnectionId, field.Transformations.Select(step => step.Key).ToList()))
            .ToList());

    private Dictionary<string, string[]> Validate(IReadOnlyList<FieldReferences> fields)
    {
        var errors = new Dictionary<string, string[]>();
        var existing = fields
            .Where(field => field.ConnectionId is not null)
            .Select(field => field.ConnectionId.Value)
            .Distinct()
            .ToDictionary(id => id, id => connections.Get(id) is not null);
        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            if (field.ConnectionId is { } connectionId && !existing[connectionId])
            {
                errors[$"fields[{index}].connectionId"] = [$"Połączenie '{connectionId}' nie istnieje."];
            }

            for (var position = 0; position < field.TransformationKeys.Count; position++)
            {
                var key = field.TransformationKeys[position];
                if (!catalog.Contains(key))
                {
                    errors[$"fields[{index}].transformations[{position}].key"] = [$"Transformacja '{key}' nie istnieje w katalogu."];
                }
            }
        }

        return errors;
    }

    private sealed record FieldReferences(Guid? ConnectionId, IReadOnlyList<string> TransformationKeys);
}
