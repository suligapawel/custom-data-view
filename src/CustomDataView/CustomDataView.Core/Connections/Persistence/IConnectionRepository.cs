using CustomDataView.Core.Connections.Definitions;

namespace CustomDataView.Core.Connections.Persistence;

public interface IConnectionRepository
{
    void Add(Connection connection);
    Connection Get(Guid id);
    IReadOnlyList<Connection> GetAll();
}
