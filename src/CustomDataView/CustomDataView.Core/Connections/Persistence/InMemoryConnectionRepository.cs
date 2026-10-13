using CustomDataView.Core.Connections.Definitions;

namespace CustomDataView.Core.Connections.Persistence;

public sealed class InMemoryConnectionRepository : IConnectionRepository
{
    private static readonly Dictionary<Guid, Connection> Connections = new();
    private static readonly List<Guid> AddingOrder = [];
    private static readonly Lock Sync = new();

    public void Add(Connection connection)
    {
        lock (Sync)
        {
            Connections.Add(connection.Id, connection);
            AddingOrder.Add(connection.Id);
        }
    }

    public Connection Get(Guid id)
    {
        lock (Sync)
        {
            return Connections.GetValueOrDefault(id);
        }
    }

    public void Update(Connection connection)
    {
        lock (Sync)
        {
            Connections[connection.Id] = connection;
        }
    }

    public void Delete(Guid id)
    {
        lock (Sync)
        {
            Connections.Remove(id);
            AddingOrder.Remove(id);
        }
    }

    public IReadOnlyList<Connection> GetAll()
    {
        lock (Sync)
        {
            return AddingOrder.Select(id => Connections[id]).ToList();
        }
    }
}
