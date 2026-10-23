using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Combinations;

public sealed class JoinCombination : Combination
{
    private static readonly CombinationParameterDescriptor SeparatorParameter =
        new("separator", TransformationParameterTypes.String, false);

    public override string Key => "join";

    public override string Name => "Złączenie";

    public override string Description => "Łączy wartości pól w kolejności listy separatorem; pomija brak wartości i rozkłada listy.";

    public override CombinationParameterDescriptor Parameter => SeparatorParameter;
}
