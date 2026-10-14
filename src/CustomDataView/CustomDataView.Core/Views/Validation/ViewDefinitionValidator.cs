using CustomDataView.Core.Views.Definitions;

namespace CustomDataView.Core.Views.Validation;

public static class ViewDefinitionValidator
{
    private const int MaxNameLength = 100;

    public static IDictionary<string, string[]> Validate(ViewInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > MaxNameLength)
        {
            errors["name"] = [$"Nazwa widoku jest wymagana i może mieć najwyżej {MaxNameLength} znaków."];
        }

        var tileIds = new HashSet<Guid>();
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

            if (input.Tiles[index].Position is null or < 0)
            {
                errors[$"tiles[{index}].position"] = ["Pozycja kafelka jest wymagana i nie może być mniejsza niż 0."];
            }
        }

        return errors;
    }
}
