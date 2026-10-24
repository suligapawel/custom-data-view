using System.Threading.Channels;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Dictionaries;

public sealed class DictionaryLoadQueue : IDictionaryLoadQueue
{
    private readonly Channel<IReadOnlyList<DictionaryReference>> channel =
        Channel.CreateUnbounded<IReadOnlyList<DictionaryReference>>(new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(IEnumerable<DictionaryReference> references)
    {
        var batch = references.ToList();
        if (batch.Count > 0)
        {
            channel.Writer.TryWrite(batch);
        }
    }
}
