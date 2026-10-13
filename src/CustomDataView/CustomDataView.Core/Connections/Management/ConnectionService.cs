using System.Text.Json.Nodes;
using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Persistence;
using CustomDataView.Core.Exceptions;

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
                MethodOf(connection.Definition),
                connection.Definition.Url))
            .ToList();
    }

    public ConnectionDto Get(Guid id)
    {
        var connection = repository.Get(id) ?? throw new NotFoundException(id);
        var definition = connection.Definition;
        var post = definition as PostConnectionDefinition;
        return new ConnectionDto(
            connection.Id,
            definition.Name,
            MethodOf(definition),
            definition.Url,
            definition.Headers,
            ToDtos(definition.PathParameters),
            ToDtos(definition.QueryParameters),
            ParseJson(post?.Body),
            ToDtos(post?.BodyParameters ?? []));
    }

    public void Update(Guid id, ConnectionDefinition definition)
    {
        var connection = repository.Get(id) ?? throw new NotFoundException(id);
        repository.Update(connection with { Definition = definition });
    }

    public void Delete(Guid id)
    {
        _ = repository.Get(id) ?? throw new NotFoundException(id);
    }

    private static List<ParameterDeclarationDto> ToDtos(IReadOnlyList<ParameterDeclaration> parameters)
    {
        return parameters
            .Select(parameter => new ParameterDeclarationDto(parameter.Name, parameter.Required, ParseJson(parameter.DefaultValue)))
            .ToList();
    }

    private static JsonNode ParseJson(string json)
    {
        return json is null ? null : JsonNode.Parse(json);
    }

    private static string MethodOf(ConnectionDefinition definition)
    {
        return definition is PostConnectionDefinition ? "POST" : "GET";
    }
}
