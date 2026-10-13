using CustomDataView.Core.Connections.Http;

namespace CustomDataView.Core.Connections.Calls;

public interface IConnection
{
    Task<ExternalApiResult> Get(ConnectionParameterValues parameters, CancellationToken cancellationToken);
}
