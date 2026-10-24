using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;

namespace CustomDataView.Domain.ValueObjects;

public sealed record TileBinding(string From, string To, IReadOnlyList<TileBindingMapping> Map)
{
    public bool IsFromParent => From == TileNodeKeys.Parent;

    public bool HasDistinctEnds
        => !string.IsNullOrWhiteSpace(To) && To.Split(TileNodeKeys.PathSeparator)[0] != From?.Split(TileNodeKeys.PathSeparator)[0];

    public string[] FromSegments => From.Split(TileNodeKeys.PathSeparator);

    public string[] ToSegments => To.Split(TileNodeKeys.PathSeparator);

    public void Check(string path, DefinitionViolations violations)
    {
        if (string.IsNullOrWhiteSpace(From))
        {
            violations.Set($"{path}.from", "Węzeł źródłowy wiązania jest wymagany.");
        }

        if (string.IsNullOrWhiteSpace(To))
        {
            violations.Set($"{path}.to", "Węzeł docelowy wiązania jest wymagany.");
        }
        else if (!HasDistinctEnds)
        {
            violations.Set($"{path}.to", "Węzeł docelowy musi być różny od źródłowego.");
        }

        if (Map.Count == 0)
        {
            violations.Set($"{path}.map", "Wiązanie musi mieć co najmniej jedno mapowanie.");
        }

        for (var position = 0; position < Map.Count; position++)
        {
            var mapping = Map[position];
            var mappingPath = $"{path}.map[{position}]";
            if (string.IsNullOrWhiteSpace(mapping.Field))
            {
                violations.Set($"{mappingPath}.field", "Pole źródłowe mapowania jest wymagane.");
            }

            if (!mapping.TargetsExactlyOne)
            {
                violations.Set(mappingPath, "Mapowanie wskazuje dokładnie jedno z: parametr albo wejście danych.");
            }

            if (mapping.Scope is not null && !BindingScopes.Values.Contains(mapping.Scope))
            {
                violations.Set($"{mappingPath}.scope", $"Zakres mapowania musi mieć jedną z wartości: {string.Join(", ", BindingScopes.Values)}.");
            }
        }
    }
}
