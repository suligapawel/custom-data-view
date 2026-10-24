using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;

namespace CustomDataView.Domain.ValueObjects;

public sealed record TileAction(string Key, string Label, string Type, Guid TileId, string Scope, IReadOnlyList<TileActionMapping> Map)
{
    public void Check(string path, DefinitionViolations violations)
    {
        if (!TileNodeKeys.IsValid(Key))
        {
            violations.Set($"{path}.key", $"Klucz akcji jest wymagany, może mieć najwyżej {TileNodeKeys.MaxLength} znaków i nie może zawierać znaku '/'.");
        }

        if (!TileTexts.IsValid(Label))
        {
            violations.Set($"{path}.label", $"Etykieta akcji jest wymagana i może mieć najwyżej {TileTexts.MaxLength} znaków.");
        }

        if (!TileActionTypes.All.Contains(Type))
        {
            violations.Set($"{path}.type", $"Typ akcji musi mieć jedną z wartości: {string.Join(", ", TileActionTypes.All)}.");
        }

        if (TileId == Guid.Empty)
        {
            violations.Set($"{path}.tileId", "Kafelek docelowy akcji jest wymagany.");
        }

        if (!TileActionScopes.All.Contains(Scope))
        {
            violations.Set($"{path}.scope", $"Zakres akcji musi mieć jedną z wartości: {string.Join(", ", TileActionScopes.All)}.");
        }
    }
}
