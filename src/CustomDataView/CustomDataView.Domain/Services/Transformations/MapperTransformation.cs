using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Exceptions;
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
        if (!DictionaryReference.TryParse(parameter, out var reference))
        {
            throw InvalidParameter();
        }

        if (!dictionaries.TryGet(reference, out var dictionary))
        {
            throw new TransformationFailedException(Key, TransformationFailureReasons.DictionaryUnavailable);
        }

        if (!MapperDictionary.TryKey(value, out var key))
        {
            throw new TransformationFailedException(Key, TransformationFailureReasons.InvalidValue);
        }

        return dictionary.TryGet(key, out var mapped)
            ? mapped
            : throw new TransformationFailedException(Key, TransformationFailureReasons.MissingDictionaryKey);
    }
}
