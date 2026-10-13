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
            .Select(connection => new ConnectionSummaryDto(
                connection.Id,
                connection.Definition.Name,
                MethodOf(connection.Definition)))
            .ToList();
    }

    public ConnectionDto Get(Guid id)
    {
        var connection = repository.Get(id);
        var body = (connection.Definition as PostConnectionDefinition)?.Body;
        return new ConnectionDto(connection.Id, body is null ? null : JsonNode.Parse(body));
    }

    private static string MethodOf(ConnectionDefinition definition)
    {
        return definition is PostConnectionDefinition ? "POST" : "GET";
    }
}
