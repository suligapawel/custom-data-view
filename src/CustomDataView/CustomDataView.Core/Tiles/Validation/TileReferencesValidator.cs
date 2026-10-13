using CustomDataView.Core.Connections.Persistence;
using CustomDataView.Core.Tiles.Definitions;
using CustomDataView.Core.Transformations.Abstractions;

namespace CustomDataView.Core.Tiles.Validation;

public sealed class TileReferencesValidator(IConnectionRepository connections, IEnumerable<ITransformation> transformations)
{
    private readonly HashSet<string> catalog =
        transformations.Select(transformation => transformation.Key).ToHashSet(StringComparer.Ordinal);

    public IDictionary<string, string[]> Validate(TileDefinition definition)
    {
        var errors = new Dictionary<string, string[]>();
        var existing = definition.Fields
            .Select(field => field.ConnectionId)
            .Distinct()
            .ToDictionary(id => id, id => connections.Get(id) is not null);
        for (var index = 0; index < definition.Fields.Count; index++)
        {
            var field = definition.Fields[index];
            if (!existing[field.ConnectionId])
            {
                errors[$"fields[{index}].connectionId"] = [$"Połączenie '{field.ConnectionId}' nie istnieje."];
            }

            for (var position = 0; position < field.Transformations.Count; position++)
            {
                var key = field.Transformations[position].Key;
                if (!catalog.Contains(key))
                {
                    errors[$"fields[{index}].transformations[{position}].key"] = [$"Transformacja '{key}' nie istnieje w katalogu."];
                }
            }
        }

        return errors;
    }
}
