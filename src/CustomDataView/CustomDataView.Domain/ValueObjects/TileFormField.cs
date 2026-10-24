using System.Text.Json.Nodes;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;

namespace CustomDataView.Domain.ValueObjects;

public sealed record TileFormField(string Label, string Type, bool Required, JsonNode Default)
{
    public void Check(string path, DefinitionViolations violations)
    {
        if (!TileFormFieldTypes.All.Contains(Type))
        {
            violations.Set($"{path}.type", $"Typ pola formularza jest wymagany i musi mieć jedną z wartości: {string.Join(", ", TileFormFieldTypes.All)}.");
        }
        else if (Default is not null && !TileFormFieldTypes.Matches(Type, Default))
        {
            violations.Set($"{path}.default", $"Wartość domyślna musi być zgodna z typem '{Type}'.");
        }
    }
}
