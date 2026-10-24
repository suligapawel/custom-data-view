using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Transformations;

public sealed class MapperTransformation : Transformation
{
    private static readonly TransformationParameterDescriptor DictionaryParameter =
        new("dictionary", TransformationParameterTypes.String);

    public override string Key => "mapper";

    public override string Name => "Słownik";

    public override string Description => "Zamienia wartość na wartość ze słownika z kafelka.";

    public override TransformationParameterDescriptor Parameter => DictionaryParameter;

    protected override object Transform(object value, string parameter) => value;
}
