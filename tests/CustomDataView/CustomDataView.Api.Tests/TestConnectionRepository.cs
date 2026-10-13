using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Persistence;

namespace CustomDataView.Api.Tests;

public sealed class TestConnectionRepository : IConnectionRepository
{
    private readonly List<Connection> connections = [];

    public void Add(Connection connection)
    {
        connections.Add(connection);
    }

    public Connection Get(Guid id) => connections.SingleOrDefault(connection => connection.Id == id);

    public void Update(Connection connection)
    {
        var index = connections.FindIndex(stored => stored.Id == connection.Id);
        connections[index] = connection;
    }

    public void Delete(Guid id)
    {
        connections.RemoveAll(connection => connection.Id == id);
    }

    public IReadOnlyList<Connection> GetAll() => connections.ToList();
}
