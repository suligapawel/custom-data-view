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

        for (var index = 0; index < input.Tiles.Count; index++)
        {
            if (input.Tiles[index].TileId is null)
            {
                errors[$"tiles[{index}].tileId"] = ["Kafelek jest wymagany."];
            }
        }

        return errors;
    }
}
