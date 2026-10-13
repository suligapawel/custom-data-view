using CustomDataView.Core.Connections.Persistence;
using CustomDataView.Core.Exceptions;

namespace CustomDataView.Core.Connections.Testing;

public sealed class ConnectionTestService(IConnectionRepository repository) : IConnectionTestService
{
    public void Test(Guid id)
    {
        _ = repository.Get(id) ?? throw new NotFoundException(id);
    }
}
