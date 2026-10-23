using CustomDataView.Application.Connections.Validation;
using CustomDataView.Application.Exceptions;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Connections.Calls;

public static class ConnectionTester
{
    public static Task<ExternalApiResult> Test(
        IConnectionFactory factory,
        ConnectionDefinition definition,
        ParameterValuesInput parameters,
        CancellationToken cancellationToken)
    {
        var errors = ParameterValuesValidator.Validate(definition, parameters);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return factory.Create(definition).Get(parameters.ToValues(), cancellationToken);
    }
}
