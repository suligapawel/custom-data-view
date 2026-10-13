using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Http;

namespace CustomDataView.Core.Connections.Calls;

public sealed class ConnectionFactory(IExternalApiClient client) : IConnectionFactory
{
    public IConnection Create(ConnectionDefinition definition)
    {
        return definition switch
        {
            GetConnectionDefinition get => new GetConnection(get, client),
            PostConnectionDefinition post => new PostConnection(post, client),
            _ => throw new NotSupportedException($"Connection definition {definition.GetType().Name} is not supported.")
        };
    }
}
