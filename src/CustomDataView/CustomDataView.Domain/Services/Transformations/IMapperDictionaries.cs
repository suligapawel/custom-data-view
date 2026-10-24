using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Transformations;

public interface IMapperDictionaries
{
    bool TryGet(DictionaryReference reference, out MapperDictionary dictionary);
}
