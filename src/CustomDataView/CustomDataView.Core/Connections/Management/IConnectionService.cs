namespace CustomDataView.Core.Connections.Management;

public interface IConnectionService
{
    Guid Add();
    ConnectionDto Get(Guid id);
}
