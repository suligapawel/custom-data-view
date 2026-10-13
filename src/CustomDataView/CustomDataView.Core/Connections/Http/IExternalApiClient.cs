namespace CustomDataView.Core.Connections.Http;

public interface IExternalApiClient
{
    Task<ExternalApiResult> Send(HttpRequestMessage request, CancellationToken cancellationToken);
}
