using CustomDataView.Application.Connections.Calls;

namespace CustomDataView.Infrastructure.ExternalApi;

public interface IExternalApiClient
{
    Task<ExternalApiResult> Send(HttpRequestMessage request, CancellationToken cancellationToken);
}
