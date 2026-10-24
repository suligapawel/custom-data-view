using CustomDataView.Domain.Services.Combinations;

namespace CustomDataView.Domain.Services;

public sealed class CombinationService
{
    private readonly Dictionary<string, ICombination> _combinations;

    public CombinationService(IEnumerable<ICombination> combinations)
    {
        _combinations = combinations.ToDictionary(combination => combination.Key, StringComparer.Ordinal);
    }

    public ICombination Find(string key) => key is null ? null : _combinations.GetValueOrDefault(key);
}
