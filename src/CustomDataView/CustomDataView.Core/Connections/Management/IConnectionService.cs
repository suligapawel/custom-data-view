using CustomDataView.Core.Connections.Definitions;

namespace CustomDataView.Core.Connections.Management;

public interface IConnectionService
{
    Guid Add(ConnectionDefinition definition);
    IReadOnlyList<ConnectionSummaryDto> GetAll();
    ConnectionDto Get(Guid id);
    void Update(Guid id, ConnectionDefinition definition);
    void Delete(Guid id);
}
