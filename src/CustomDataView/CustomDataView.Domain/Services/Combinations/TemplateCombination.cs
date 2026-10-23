using System.Text;
using System.Text.Json.Nodes;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Combinations;

public sealed class TemplateCombination : Combination
{
    private static readonly CombinationParameterDescriptor PatternParameter =
        new("pattern", TransformationParameterTypes.String, true);

    public override string Key => "template";

    public override string Name => "Szablon tekstu";

    public override string Description => "Składa tekst ze wzorca z etykietami pól w klamrach, np. \"{Ulica} {Nr}, {Miasto}\"; {{ i }} to dosłowne klamry.";

    public override CombinationParameterDescriptor Parameter => PatternParameter;

    public override int MinFields => 0;

    public override int? MaxFields => 0;

    public override bool AcceptsCollections => false;

    public override IReadOnlyList<CombinationError> Validate(string parameter, IReadOnlyList<string> fields)
    {
        var errors = base.Validate(parameter, fields).ToList();
        if (parameter is not null && (!TemplatePattern.TryParse(parameter, out var pattern) || pattern.Labels.Count == 0))
        {
            errors.Add(new CombinationError(
                CombinationError.ParameterTarget,
                "Wzorzec musi zawierać co najmniej jedną etykietę pola w klamrach; dosłowne klamry zapisuje się jako {{ i }}."));
        }

        return errors;
    }

    public override IReadOnlyList<string> Inputs(string parameter, IReadOnlyList<string> fields)
        => TemplatePattern.TryParse(parameter, out var pattern) ? pattern.Labels.Distinct(StringComparer.OrdinalIgnoreCase).ToList() : [];

    public override JsonNode Combine(string parameter, IReadOnlyList<CombinationInput> inputs)
    {
        TemplatePattern.TryParse(parameter, out var pattern);
        var text = new StringBuilder();
        foreach (var part in pattern.Parts)
        {
            text.Append(part.IsLabel
                ? CombinationValue.ToText(Key, inputs.First(input => TileLabels.AreEqual(input.Label, part.Text)).Value)
                : part.Text);
        }

        return JsonValue.Create(text.ToString());
    }
}
