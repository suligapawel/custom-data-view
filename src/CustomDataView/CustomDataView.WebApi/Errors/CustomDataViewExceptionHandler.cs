using CustomDataView.Application.Exceptions;
using CustomDataView.Domain.Exceptions;
using CustomDataView.WebApi.Responses;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace CustomDataView.WebApi.Errors;

public sealed class CustomDataViewExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is NotFoundException)
        {
            await Results.NotFound().ExecuteAsync(httpContext);
            return true;
        }

        if (exception is ValidationException validation)
        {
            await Results.ValidationProblem(validation.Errors).ExecuteAsync(httpContext);
            return true;
        }

        if (exception is ConnectionInUseException inUse)
        {
            await Results.Conflict(new ConnectionInUseResponse(ConnectionInUseResponse.ConnectionInUseCode, inUse.Tiles))
                .ExecuteAsync(httpContext);
            return true;
        }

        if (exception is BadHttpRequestException badRequest)
        {
            await Results.Problem(statusCode: badRequest.StatusCode, title: "Nieprawidłowe żądanie.").ExecuteAsync(httpContext);
            return true;
        }

        return false;
    }
}
