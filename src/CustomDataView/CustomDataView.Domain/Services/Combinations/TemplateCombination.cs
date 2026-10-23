using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Combinations;

public sealed class TemplateCombination : Combination
{
    private static readonly CombinationParameterDescriptor PatternParameter =
        new("pattern", TransformationParameterTypes.String, true);

    public override string Key => "template";

    public override string Name => "Szablon tekstu";

    public override string Description => "Składa tekst ze wzorca z etykietami pól w klamrach, np. \"{Ulica} {Nr}, {Miasto}\"; {{ i }} to dosłowne klamry.";

    public override CombinationParameterDescriptor Parameter => PatternParameter;

    public override int MinFields => 0;

    public override int? MaxFields => 0;
}
