using CustomDataView.Application.Connections.Calls;
using CustomDataView.Application.Connections.Commands;
using CustomDataView.Application.Connections.Dtos;
using CustomDataView.Application.Connections.Queries;
using CustomDataView.WebApi.Requests;
using CustomDataView.WebApi.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace CustomDataView.WebApi.Endpoints;

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
            endpoints.MapPost(
                ConnectionsUrl,
                async (ConnectionRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
                {
                    var errors = request.Validate();
                    if (errors.Count > 0)
                    {
                        return Results.ValidationProblem(errors);
                    }

                    var id = await bus.InvokeAsync<Guid>(new AddConnection(request.ToDefinition()), cancellationToken);
                    return Results.Created($"{ConnectionsUrl}/{id}", new ConnectionCreatedResponse(id));
                });
            return endpoints;
        }

        private IEndpointRouteBuilder GetConnections()
        {
            endpoints.MapGet(
                ConnectionsUrl,
                async (IMessageBus bus, CancellationToken cancellationToken)
                    => Results.Ok(await bus.InvokeAsync<IReadOnlyList<ConnectionSummaryDto>>(new GetConnections(), cancellationToken)));
            return endpoints;
        }

        private IEndpointRouteBuilder GetConnection()
        {
            endpoints.MapGet(
                ConnectionUrl,
                async (Guid id, IMessageBus bus, CancellationToken cancellationToken)
                    => Results.Ok(await bus.InvokeAsync<ConnectionDto>(new GetConnection(id), cancellationToken)));
            return endpoints;
        }

        private IEndpointRouteBuilder UpdateConnection()
        {
            endpoints.MapPut(
                ConnectionUrl,
                async (Guid id, ConnectionRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
                {
                    var errors = request.Validate();
                    if (errors.Count > 0)
                    {
                        return Results.ValidationProblem(errors);
                    }

                    await bus.InvokeAsync(new UpdateConnection(id, request.ToDefinition()), cancellationToken);
                    return Results.NoContent();
                });
            return endpoints;
        }

        private IEndpointRouteBuilder DeleteConnection()
        {
            endpoints.MapDelete(
                ConnectionUrl,
                async (Guid id, IMessageBus bus, CancellationToken cancellationToken) =>
                {
                    await bus.InvokeAsync(new DeleteConnection(id), cancellationToken);
                    return Results.NoContent();
                });
            return endpoints;
        }

        private IEndpointRouteBuilder TestUnsavedConnection()
        {
            endpoints.MapPost(
                $"{ConnectionsUrl}/test",
                async (ConnectionDraftTestRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
                {
                    var errors = request.Validate();
                    if (errors.Count > 0)
                    {
                        return Results.ValidationProblem(errors);
                    }

                    var query = new TestConnectionDraft(
                        request.Connection.ToDefinition(),
                        ConnectionTestRequest.ValuesOf(request.Parameters));
                    return ConnectionTestResponses.From(await bus.InvokeAsync<ExternalApiResult>(query, cancellationToken));
                });
            return endpoints;
        }

        private IEndpointRouteBuilder TestSavedConnection()
        {
            endpoints.MapPost(
                $"{ConnectionUrl}/test",
                async (Guid id, ConnectionTestRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
                {
                    var query = new TestConnection(id, ConnectionTestRequest.ValuesOf(request));
                    return ConnectionTestResponses.From(await bus.InvokeAsync<ExternalApiResult>(query, cancellationToken));
                });
            return endpoints;
        }
    }
}
