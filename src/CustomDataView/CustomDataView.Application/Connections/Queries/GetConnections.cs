using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Connections.Dtos;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Repositories;

namespace CustomDataView.Application.Connections.Queries;

public sealed record GetConnections : IQuery<IReadOnlyList<ConnectionSummaryDto>>;

public sealed class GetConnectionsHandler(IConnectionRepository repository)
{
    public IReadOnlyList<ConnectionSummaryDto> Handle(GetConnections query)
        => repository.GetAll()
            .Select(connection => new ConnectionSummaryDto(
                connection.Id,
                connection.Definition.Name,
                connection.Definition.Method,
                connection.Definition.Url))
            .ToList();
}
