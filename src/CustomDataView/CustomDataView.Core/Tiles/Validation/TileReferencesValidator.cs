using CustomDataView.Core.Connections.Persistence;
using CustomDataView.Core.Tiles.Definitions;

namespace CustomDataView.Core.Tiles.Validation;

public sealed class TileReferencesValidator(IConnectionRepository connections)
{
    public IDictionary<string, string[]> Validate(TileDefinition definition)
    {
        var errors = new Dictionary<string, string[]>();
        var existing = definition.Fields
            .Select(field => field.ConnectionId)
            .Distinct()
            .ToDictionary(id => id, id => connections.Get(id) is not null);
        for (var index = 0; index < definition.Fields.Count; index++)
        {
            var connectionId = definition.Fields[index].ConnectionId;
            if (!existing[connectionId])
            {
                errors[$"fields[{index}].connectionId"] = [$"Połączenie '{connectionId}' nie istnieje."];
            }
        }

        return errors;
    }
}
