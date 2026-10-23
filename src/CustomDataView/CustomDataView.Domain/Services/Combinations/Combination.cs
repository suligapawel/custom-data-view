using System.Text.Json.Nodes;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Combinations;

public abstract class Combination : ICombination
{
    public abstract string Key { get; }

    public abstract string Name { get; }

    public abstract string Description { get; }

    public virtual CombinationParameterDescriptor Parameter => null;

    public virtual int MinFields => 1;

    public virtual int? MaxFields => null;

    public virtual bool AcceptsCollections => true;

    public virtual bool AcceptsTransformations => true;

    public virtual IReadOnlyList<string> Inputs(string parameter, IReadOnlyList<string> fields) => fields;

    public virtual IReadOnlyList<CombinationError> Validate(string parameter, IReadOnlyList<string> fields)
    {
        var errors = new List<CombinationError>();
        if (Parameter is null && parameter is not null)
        {
            errors.Add(new CombinationError(CombinationError.ParameterTarget, $"Kombinator '{Key}' nie ma parametru."));
        }
        else if (Parameter is { Required: true } && string.IsNullOrEmpty(parameter))
        {
            errors.Add(new CombinationError(CombinationError.ParameterTarget, $"Parametr '{Parameter.Name}' kombinatora '{Key}' jest wymagany."));
        }

        if (MaxFields == 0 && fields.Count > 0)
        {
            errors.Add(new CombinationError(CombinationError.FieldsTarget, $"Kombinator '{Key}' nie przyjmuje listy pól."));
        }
        else if (fields.Count < MinFields || fields.Count > MaxFields)
        {
            errors.Add(new CombinationError(
                CombinationError.FieldsTarget,
                MaxFields is { } max
                    ? $"Kombinator '{Key}' wymaga od {MinFields} do {max} pól."
                    : $"Kombinator '{Key}' wymaga co najmniej {MinFields} pól."));
        }

        return errors;
    }

    public abstract JsonNode Combine(string parameter, IReadOnlyList<CombinationInput> inputs);
}
