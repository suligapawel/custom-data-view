using CustomDataView.Core.Tiles.Definitions;

namespace CustomDataView.Core.Tiles.Validation;

public static class TileDefinitionValidator
{
    private const int MaxNameLength = 100;

    public static IDictionary<string, string[]> Validate(TileInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > MaxNameLength)
        {
            errors["name"] = [$"Nazwa kafelka jest wymagana i może mieć najwyżej {MaxNameLength} znaków."];
        }

        return errors;
    }
}
