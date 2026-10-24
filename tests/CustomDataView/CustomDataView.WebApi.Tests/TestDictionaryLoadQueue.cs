using CustomDataView.Application.Dictionaries;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.WebApi.Tests;

public sealed class TestDictionaryLoadQueue(DictionaryLoader loader) : IDictionaryLoadQueue
{
    private readonly List<DictionaryReference> pending = [];

    public void Enqueue(IEnumerable<DictionaryReference> references)
    {
        lock (pending)
        {
            pending.AddRange(references);
        }
    }

    public Task ProcessPending()
    {
        List<DictionaryReference> references;
        lock (pending)
        {
            references = [.. pending];
            pending.Clear();
        }

        return loader.Load(references, CancellationToken.None);
    }

    public void Clear()
    {
        lock (pending)
        {
            pending.Clear();
        }
    }
}
