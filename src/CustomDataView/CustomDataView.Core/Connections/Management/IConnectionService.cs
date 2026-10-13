namespace CustomDataView.Core.Connections.Management;

public interface IConnectionService
{
    Guid Add(string body);
    ConnectionDto Get(Guid id);
}
