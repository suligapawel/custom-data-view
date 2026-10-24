using CustomDataView.Application.Tiles.Inputs;
using CustomDataView.Domain.Policies;

namespace CustomDataView.Application.Tiles.Validation;

public static class TileInputValidator
{
    public static IDictionary<string, string[]> Validate(TileInput input)
    {
        var errors = new Dictionary<string, string[]>(input.ToDefinition().Check());
        if (input.IsForm)
        {
            ValidateFormFields(input, errors);
            return errors;
        }

        if (!input.IsTabs)
        {
            ValidateTransformationParameters(input, errors);
        }

        for (var index = 0; index < input.Children.Count; index++)
        {
            if (input.Children[index].Position is null)
            {
                errors[$"children[{index}].position"] = ["Pozycja dziecka jest wymagana i nie może być mniejsza niż 0."];
            }
        }

        return errors;
    }

    private static void ValidateFormFields(TileInput input, Dictionary<string, string[]> errors)
    {
        for (var index = 0; index < input.Fields.Count; index++)
        {
            var field = input.Fields[index];
            if (field.Source is not null || field.Path is not null || field.Transformations.Count > 0 || field.Hidden || field.Role is not null)
            {
                errors[$"fields[{index}]"] = ["Pole formularza ma tylko etykietę, typ, wymaganie i wartość domyślną."];
            }
        }
    }

    private static void ValidateTransformationParameters(TileInput input, Dictionary<string, string[]> errors)
    {
        for (var index = 0; index < input.Fields.Count; index++)
        {
            var transformations = input.Fields[index].Transformations;
            for (var position = 0; position < transformations.Count; position++)
            {
                if (!TransformationParameterValues.IsStorable(transformations[position].Parameter))
                {
                    errors[$"fields[{index}].transformations[{position}].parameter"] =
                        ["Wartość parametru transformacji musi być tekstem lub liczbą."];
                }
            }
        }
    }
}
