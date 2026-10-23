using CustomDataView.Application.Exceptions;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Views.Validation;

public sealed class ViewReferencesValidator(ITileRepository tiles)
{
    public void EnsureValid(ViewDefinition definition)
    {
        var errors = Validate(definition);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    public IDictionary<string, string[]> Validate(ViewDefinition definition)
    {
        var errors = new Dictionary<string, string[]>();
        var existing = tiles.GetReferences(definition.Tiles.Select(tile => tile.TileId).Distinct().ToList())
            .Select(tile => tile.Id)
            .ToHashSet();
        for (var index = 0; index < definition.Tiles.Count; index++)
        {
            var tileId = definition.Tiles[index].TileId;
            if (!existing.Contains(tileId))
            {
                errors[$"tiles[{index}].tileId"] = [$"Kafelek '{tileId}' nie istnieje."];
            }
        }

        return errors;
    }
}
