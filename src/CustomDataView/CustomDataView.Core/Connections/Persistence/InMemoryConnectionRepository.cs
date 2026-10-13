using CustomDataView.Core.Connections.Definitions;

namespace CustomDataView.Core.Connections.Persistence;

public sealed class InMemoryConnectionRepository : IConnectionRepository
{
    private static readonly Dictionary<Guid, Connection> Connections = new();
    private static readonly Lock Sync = new();

    public void Add(Connection connection)
    {
        lock (Sync)
        {
            Connections.Add(connection.Id, connection);
        }
    }

    public Connection Get(Guid id)
    {
        lock (Sync)
        {
            return Connections.GetValueOrDefault(id);
        }
    }
}
