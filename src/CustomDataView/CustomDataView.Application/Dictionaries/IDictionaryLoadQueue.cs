using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Dictionaries;

public interface IDictionaryLoadQueue
{
    void Enqueue(IEnumerable<DictionaryReference> references);
}
