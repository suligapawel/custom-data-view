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

    public Connection Get(Guid id)
    {
        return connections.SingleOrDefault(connection => connection.Id == id);
    }

    public IReadOnlyList<Connection> GetAll()
    {
        return connections.ToList();
    }
}
