using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Connections.Calls;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Connections.Queries;

public sealed record TestConnectionDraft(ConnectionDefinition Definition, ParameterValuesInput Parameters)
    : IQuery<ExternalApiResult>;

public sealed class TestConnectionDraftHandler(IConnectionFactory factory)
{
    public Task<ExternalApiResult> Handle(TestConnectionDraft query, CancellationToken cancellationToken)
        => ConnectionTester.Test(factory, query.Definition, query.Parameters, cancellationToken);
}
