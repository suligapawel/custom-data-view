using System.Text.Json.Nodes;
using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Connections.Dtos;
using CustomDataView.Application.Exceptions;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Connections.Queries;

public sealed record GetConnection(Guid Id) : IQuery<ConnectionDto>;

public sealed class GetConnectionHandler(IConnectionRepository repository)
{
    public ConnectionDto Handle(GetConnection query)
    {
        var connection = repository.Get(query.Id) ?? throw new NotFoundException(query.Id);
        var definition = connection.Definition;
        var post = definition as PostConnectionDefinition;
        return new ConnectionDto(
            connection.Id,
            definition.Name,
            definition.Method,
            definition.Url,
            ToDtos(definition.PathParameters),
            ToDtos(definition.QueryParameters),
            ParseJson(post?.Body),
            ToDtos(post?.BodyParameters ?? []));
    }

    private static List<ParameterDeclarationDto> ToDtos(IReadOnlyList<ParameterDeclaration> parameters)
        => parameters
            .Select(parameter => new ParameterDeclarationDto(parameter.Name, parameter.Required, ParseJson(parameter.DefaultValue)))
            .ToList();

    private static JsonNode ParseJson(string json) => json is null ? null : JsonNode.Parse(json);
}
