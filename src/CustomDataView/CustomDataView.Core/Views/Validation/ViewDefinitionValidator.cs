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
            var tileId = input.Tiles[index].TileId;
            if (tileId is null)
            {
                errors[$"tiles[{index}].tileId"] = ["Kafelek jest wymagany."];
            }
            else if (!tileIds.Add(tileId.Value))
            {
                errors[$"tiles[{index}].tileId"] = [$"Kafelek '{tileId}' występuje w widoku więcej niż raz."];
            }

            var position = input.Tiles[index].Position;
            if (position is null or < 0)
            {
                errors[$"tiles[{index}].position"] = ["Pozycja kafelka jest wymagana i nie może być mniejsza niż 0."];
            }
            else if (!positions.Add(position.Value))
            {
                errors[$"tiles[{index}].position"] = [$"Pozycja {position} występuje w widoku więcej niż raz."];
            }

            if (input.Tiles[index].CssClass?.Trim().Length > MaxCssClassLength)
            {
                errors[$"tiles[{index}].cssClass"] = [$"Klasa CSS może mieć najwyżej {MaxCssClassLength} znaków."];
            }
        }

        return errors;
    }
}
