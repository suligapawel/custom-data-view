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
    private const string ConnectionUrl = $"{ConnectionsUrl}/{{id:guid}}";

    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapConnectionEndpoints()
        {
            return endpoints
                .AddConnection()
                .GetConnections()
                .GetConnection()
                .UpdateConnection()
                .DeleteConnection()
                .TestUnsavedConnection()
                .TestSavedConnection();
        }

        private IEndpointRouteBuilder AddConnection()
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
            return endpoints;
        }

        private IEndpointRouteBuilder GetConnections()
        {
            endpoints.MapGet(ConnectionsUrl, (IConnectionService service) => Results.Ok(service.GetAll()));
            return endpoints;
        }

        private IEndpointRouteBuilder GetConnection()
        {
            endpoints.MapGet(ConnectionUrl, (Guid id, IConnectionService service) => Results.Ok(service.Get(id)));
            return endpoints;
        }

        private IEndpointRouteBuilder UpdateConnection()
        {
            endpoints.MapPut(ConnectionUrl, (Guid id, ConnectionRequest request, IConnectionService service) =>
            {
                var errors = request.Validate();
                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                service.Update(id, request.ToInput().ToDefinition());
                return Results.NoContent();
            });
            return endpoints;
        }

        private IEndpointRouteBuilder DeleteConnection()
        {
            endpoints.MapDelete(ConnectionUrl, (Guid id, IConnectionService service) =>
            {
                service.Delete(id);
                return Results.NoContent();
            });
            return endpoints;
        }

        private IEndpointRouteBuilder TestUnsavedConnection()
        {
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
            return endpoints;
        }

        private IEndpointRouteBuilder TestSavedConnection()
        {
            endpoints.MapPost(
                $"{ConnectionUrl}/test",
                async (Guid id, ConnectionTestRequest request, IConnectionTestService service, CancellationToken cancellationToken) =>
                {
                    var parameters = ConnectionTestRequest.ValuesOf(request);
                    return ConnectionTestResponses.From(await service.Test(id, parameters, cancellationToken));
                });
            return endpoints;
        }
    }
}
