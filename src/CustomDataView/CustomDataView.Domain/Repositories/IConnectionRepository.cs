using CustomDataView.Domain.Entities;

namespace CustomDataView.Domain.Repositories;

public interface IConnectionRepository
{
    void Add(Connection connection);
    Connection Get(Guid id);
    IReadOnlyList<Connection> GetAll();
    void Update(Connection connection);
    void Delete(Guid id);
}
