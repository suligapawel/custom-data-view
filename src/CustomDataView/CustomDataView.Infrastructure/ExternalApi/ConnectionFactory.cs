using CustomDataView.Application.Connections.Calls;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Infrastructure.ExternalApi;

public sealed class ConnectionFactory(IExternalApiClient client) : IConnectionFactory
{
    public IConnection Create(ConnectionDefinition definition)
        => definition switch
        {
            GetConnectionDefinition get => new GetConnection(get, client),
            PostConnectionDefinition post => new PostConnection(post, client),
            _ => throw new NotSupportedException($"Connection definition {definition.GetType().Name} is not supported.")
        };
}
