using CustomDataView.Core.Connections.Calls;
using CustomDataView.Core.Connections.Http;
using CustomDataView.Core.Connections.Persistence;
using CustomDataView.Core.Connections.Validation;
using CustomDataView.Core.Exceptions;

namespace CustomDataView.Core.Connections.Testing;

public sealed class ConnectionTestService(IConnectionRepository repository, IConnectionFactory factory) : IConnectionTestService
{
    public Task<ExternalApiResult> Test(Guid id, ParameterValuesInput parameters, CancellationToken cancellationToken)
    {
        var connection = repository.Get(id) ?? throw new NotFoundException(id);
        var errors = ParameterValuesValidator.Validate(connection.Definition, parameters);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return factory.Create(connection.Definition).Get(parameters.ToValues(), cancellationToken);
    }
}
