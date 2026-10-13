using System.Text.Json.Nodes;
using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Persistence;

namespace CustomDataView.Core.Connections.Management;

public sealed class ConnectionService(IConnectionRepository repository) : IConnectionService
{
    public Guid Add(ConnectionDefinition definition)
    {
        var connection = new Connection(Guid.NewGuid(), definition);
        repository.Add(connection);
        return connection.Id;
    }

    public IReadOnlyList<ConnectionSummaryDto> GetAll()
    {
        return repository.GetAll()
            .Select(connection => new ConnectionSummaryDto(connection.Id, connection.Definition.Name))
            .ToList();
    }

    public ConnectionDto Get(Guid id)
    {
        var connection = repository.Get(id);
        var body = connection.Definition.Body;
        return new ConnectionDto(connection.Id, body is null ? null : JsonNode.Parse(body));
    }
}
