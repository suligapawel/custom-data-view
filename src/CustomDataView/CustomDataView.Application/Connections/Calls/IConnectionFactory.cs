using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Connections.Calls;

public interface IConnectionFactory
{
    IConnection Create(ConnectionDefinition definition);
}
