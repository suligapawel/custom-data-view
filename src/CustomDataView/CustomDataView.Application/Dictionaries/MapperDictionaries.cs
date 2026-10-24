using System.Collections.Concurrent;
using CustomDataView.Domain.Services.Transformations;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Dictionaries;

public sealed class MapperDictionaries : IMapperDictionaries
{
    private readonly ConcurrentDictionary<DictionaryReference, MapperDictionary> dictionaries = new();

    public bool TryGet(DictionaryReference reference, out MapperDictionary dictionary)
        => dictionaries.TryGetValue(reference, out dictionary);

    public void Set(DictionaryReference reference, MapperDictionary dictionary) => dictionaries[reference] = dictionary;

    public void Clear() => dictionaries.Clear();
}
