using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Transformations;

public sealed class MapperTransformation(IMapperDictionaries dictionaries) : Transformation
{
    public const string TransformationKey = "mapper";

    private static readonly TransformationParameterDescriptor DictionaryParameter =
        new("dictionary", TransformationParameterTypes.String);

    public override string Key => TransformationKey;

    public override string Name => "Słownik";

    public override string Description => "Zamienia wartość na wartość ze słownika z kafelka.";

    public override TransformationParameterDescriptor Parameter => DictionaryParameter;

    protected override object Transform(object value, string parameter)
    {
        DictionaryReference.TryParse(parameter, out var reference);
        dictionaries.TryGet(reference, out var dictionary);
        dictionary.TryGet(TransformationValue.ToText(Key, value), out var mapped);
        return mapped;
    }
}
