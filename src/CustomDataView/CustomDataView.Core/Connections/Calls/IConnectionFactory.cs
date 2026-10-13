using CustomDataView.Core.Connections.Definitions;

namespace CustomDataView.Core.Connections.Calls;

public interface IConnectionFactory
{
    IConnection Create(ConnectionDefinition definition);
}
