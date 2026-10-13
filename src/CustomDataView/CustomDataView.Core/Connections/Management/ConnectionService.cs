using System.Text.Json.Nodes;
using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Persistence;

namespace CustomDataView.Core.Connections.Management;

public sealed class ConnectionService(IConnectionRepository repository) : IConnectionService
{
    public Guid Add(string body)
    {
        var connection = new Connection(Guid.NewGuid(), body);
        repository.Add(connection);
        return connection.Id;
    }

    public ConnectionDto Get(Guid id)
    {
        var connection = repository.Get(id);
        return new ConnectionDto(connection.Id, connection.Body is null ? null : JsonNode.Parse(connection.Body));
    }
}
