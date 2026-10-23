using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Connections.Calls;
using CustomDataView.Application.Exceptions;
using CustomDataView.Domain.Repositories;

namespace CustomDataView.Application.Connections.Queries;

public sealed record TestConnection(Guid Id, ParameterValuesInput Parameters) : IQuery<ExternalApiResult>;

public sealed class TestConnectionHandler(IConnectionRepository repository, IConnectionFactory factory)
{
    public Task<ExternalApiResult> Handle(TestConnection query, CancellationToken cancellationToken)
    {
        var connection = repository.Get(query.Id) ?? throw new NotFoundException(query.Id);
        return ConnectionTester.Test(factory, connection.Definition, query.Parameters, cancellationToken);
    }
}
