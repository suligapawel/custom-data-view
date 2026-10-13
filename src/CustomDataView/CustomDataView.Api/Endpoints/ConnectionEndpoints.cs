using CustomDataView.Api.Requests;
using CustomDataView.Api.Responses;
using CustomDataView.Core.Connections.Management;
using CustomDataView.Core.Connections.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CustomDataView.Api.Endpoints;

public static class ConnectionEndpoints
{
    private const string ConnectionsUrl = "/api/custom-data-view/connections";

    public static IEndpointRouteBuilder MapConnectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(ConnectionsUrl, (ConnectionRequest request, IConnectionService service) =>
        {
            var errors = request.Validate();
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var id = service.Add(request.ToInput().ToDefinition());
            return Results.Created($"{ConnectionsUrl}/{id}", new ConnectionCreatedResponse(id));
        });
        endpoints.MapGet(ConnectionsUrl, (IConnectionService service) => Results.Ok(service.GetAll()));
        endpoints.MapGet($"{ConnectionsUrl}/{{id:guid}}", (Guid id, IConnectionService service) =>
            Results.Ok(service.Get(id)));
        endpoints.MapPut($"{ConnectionsUrl}/{{id:guid}}", (Guid id, ConnectionRequest request, IConnectionService service) =>
        {
            var errors = request.Validate();
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            service.Update(id, request.ToInput().ToDefinition());
            return Results.NoContent();
        });
        endpoints.MapDelete($"{ConnectionsUrl}/{{id:guid}}", (Guid id, IConnectionService service) =>
        {
            service.Delete(id);
            return Results.NoContent();
        });
        endpoints.MapPost(
            $"{ConnectionsUrl}/test",
            async (ConnectionDraftTestRequest request, IConnectionTestService service, CancellationToken cancellationToken) =>
            {
                var errors = request.Validate();
                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var definition = request.Connection.ToInput().ToDefinition();
                var parameters = ConnectionTestRequest.ValuesOf(request.Parameters);
                return ConnectionTestResponses.From(await service.Test(definition, parameters, cancellationToken));
            });
        endpoints.MapPost(
            $"{ConnectionsUrl}/{{id:guid}}/test",
            async (Guid id, ConnectionTestRequest request, IConnectionTestService service, CancellationToken cancellationToken) =>
                ConnectionTestResponses.From(await service.Test(id, ConnectionTestRequest.ValuesOf(request), cancellationToken)));
        return endpoints;
    }
}
