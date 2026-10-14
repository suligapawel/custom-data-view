using CustomDataView.Core.Views.Definitions;

namespace CustomDataView.Core.Views.Validation;

public static class ViewDefinitionValidator
{
    private const int MaxNameLength = 100;
    private const int MaxCssClassLength = 200;

    public static IDictionary<string, string[]> Validate(ViewInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > MaxNameLength)
        {
            errors["name"] = [$"Nazwa widoku jest wymagana i może mieć najwyżej {MaxNameLength} znaków."];
        }

        var tileIds = new HashSet<Guid>();
        var positions = new HashSet<int>();
        for (var index = 0; index < input.Tiles.Count; index++)
        {
            ValidateTile(input.Tiles[index], $"tiles[{index}]", tileIds, positions, errors);
        }

        return errors;
    }

    private static void ValidateTile(
        ViewTileInput tile,
        string key,
        HashSet<Guid> tileIds,
        HashSet<int> positions,
        Dictionary<string, string[]> errors)
    {
        if (tile.TileId is null)
        {
            errors[$"{key}.tileId"] = ["Kafelek jest wymagany."];
        }
        else if (!tileIds.Add(tile.TileId.Value))
        {
            errors[$"{key}.tileId"] = [$"Kafelek '{tile.TileId}' występuje w widoku więcej niż raz."];
        }

        if (tile.Position is null or < 0)
        {
            errors[$"{key}.position"] = ["Pozycja kafelka jest wymagana i nie może być mniejsza niż 0."];
        }
        else if (!positions.Add(tile.Position.Value))
        {
            errors[$"{key}.position"] = [$"Pozycja {tile.Position} występuje w widoku więcej niż raz."];
        }

        if (tile.CssClass?.Trim().Length > MaxCssClassLength)
        {
            errors[$"{key}.cssClass"] = [$"Klasa CSS może mieć najwyżej {MaxCssClassLength} znaków."];
        }
    }
}
