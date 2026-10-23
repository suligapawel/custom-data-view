namespace CustomDataView.Application.Connections.Calls;

public interface IConnection
{
    Task<ExternalApiResult> Get(ConnectionParameterValues parameters, CancellationToken cancellationToken);
}
