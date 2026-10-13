using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Persistence;

namespace CustomDataView.Core.Connections.Management;

public sealed class ConnectionService(IConnectionRepository repository) : IConnectionService
{
    public Guid Add()
    {
        var connection = new Connection(Guid.NewGuid());
        repository.Add(connection);
        return connection.Id;
    }

    public ConnectionDto Get(Guid id)
    {
        var connection = repository.Get(id);
        return new ConnectionDto(connection.Id);
    }
}
