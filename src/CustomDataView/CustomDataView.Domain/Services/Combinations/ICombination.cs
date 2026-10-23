using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Combinations;

public interface ICombination
{
    string Key { get; }

    string Name { get; }

    string Description { get; }

    CombinationParameterDescriptor Parameter { get; }

    int MinFields { get; }

    int? MaxFields { get; }
}
