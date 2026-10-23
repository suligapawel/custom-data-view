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
}
