using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;

namespace CustomDataView.Domain.ValueObjects;

public sealed record TileSource(string Key, string Kind, Guid? ConnectionId)
{
    public void Check(string path, DefinitionViolations violations)
    {
        if (!TileNodeKeys.IsValid(Key))
        {
            violations.Set(
                $"{path}.key",
                $"Klucz źródła jest wymagany, może mieć najwyżej {TileNodeKeys.MaxLength} znaków, nie może zawierać znaku '/' ani być słowem '{TileNodeKeys.Parent}'.");
        }

        if (!TileSourceKinds.All.Contains(Kind))
        {
            violations.Set($"{path}.kind", $"Rodzaj źródła musi mieć jedną z wartości: {string.Join(", ", TileSourceKinds.All)}.");
        }
        else if (Kind == TileSourceKinds.Connection && ConnectionId is null)
        {
            violations.Set($"{path}.connectionId", "Połączenie źródła jest wymagane.");
        }
        else if (Kind == TileSourceKinds.Input && ConnectionId is not null)
        {
            violations.Set($"{path}.connectionId", "Wejście danych nie wskazuje połączenia.");
        }
    }
}
