using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Services.Combinations;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services;

public sealed class CombinationService
{
    private readonly Dictionary<string, ICombination> _combinations;

    public CombinationService(IEnumerable<ICombination> combinations)
    {
        _combinations = combinations.ToDictionary(combination => combination.Key, StringComparer.Ordinal);
    }

    public ICombination Find(string key) => key is null ? null : _combinations.GetValueOrDefault(key);

    public IReadOnlyList<TileField> Inputs(TileDefinition definition, TileField field)
        => Find(field.Combination.Key) is { } combination
            ? combination.Inputs(field.Combination.Parameter, field.Combination.Fields)
                .Select(label => definition.Fields.FirstOrDefault(candidate => TileLabels.AreEqual(candidate.Label, label)))
                .Where(input => input is not null)
                .ToList()
            : [];
}
