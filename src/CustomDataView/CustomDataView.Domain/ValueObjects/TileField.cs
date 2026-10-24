using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Services;

namespace CustomDataView.Domain.ValueObjects;

public sealed record TileField(
    string Source,
    string Path,
    string Label,
    IReadOnlyList<TransformationStep> Transformations,
    bool Hidden = false,
    string Role = null)
{
    public TileFieldCombination Combination { get; init; }

    public bool IsCombined => Combination is not null;

    public bool IsCollection => !IsCombined && JsonPathParser.TryParse(Path, out var path) && path.IsCollection;

    public void Check(string path, IReadOnlySet<string> sources, DefinitionViolations violations)
    {
        if (IsCombined)
        {
            if (Source is not null || Path is not null)
            {
                violations.Set(path, "Pole ma albo źródło i ścieżkę, albo kombinator.");
            }
            else if (string.IsNullOrWhiteSpace(Combination.Key))
            {
                violations.Set($"{path}.combine.key", "Klucz kombinatora jest wymagany.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(Source))
        {
            violations.Set($"{path}.source", "Źródło pola jest wymagane.");
        }
        else if (!sources.Contains(Source))
        {
            violations.Set($"{path}.source", $"Źródło '{Source}' nie istnieje w kafelku.");
        }

        if (!JsonPathParser.TryParse(Path, out _))
        {
            violations.Set($"{path}.path", $"Ścieżka pola {path} jest wymagana i musi być poprawną ścieżką JSONPath.");
        }
    }
}
