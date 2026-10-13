using CustomDataView.Core.Connections.Calls;
using CustomDataView.Core.Connections.Http;

namespace CustomDataView.Core.Connections.Testing;

public interface IConnectionTestService
{
    Task<ExternalApiResult> Test(Guid id, ParameterValuesInput parameters, CancellationToken cancellationToken);
}
