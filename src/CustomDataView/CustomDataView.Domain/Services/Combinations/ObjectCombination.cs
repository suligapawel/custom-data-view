using System.Text.Json.Nodes;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Combinations;

public sealed class ObjectCombination : Combination
{
    public override string Key => "object";

    public override string Name => "Obiekt";

    public override string Description => "Grupuje pola w obiekt, którego kluczami są etykiety pól.";

    public override bool AcceptsTransformations => false;

    public override IReadOnlyList<CombinationError> Validate(string parameter, IReadOnlyList<string> fields)
    {
        var errors = base.Validate(parameter, fields).ToList();
        if (fields.Select(TileLabels.Normalize).Distinct().Count() != fields.Count)
        {
            errors.Add(new CombinationError(CombinationError.FieldsTarget, "Pola obiektu muszą mieć różne etykiety."));
        }

        return errors;
    }

    public override JsonNode Combine(string parameter, IReadOnlyList<CombinationInput> inputs)
    {
        var result = new JsonObject();
        foreach (var input in inputs)
        {
            result[input.Label] = input.Value?.DeepClone();
        }

        return result;
    }
}
