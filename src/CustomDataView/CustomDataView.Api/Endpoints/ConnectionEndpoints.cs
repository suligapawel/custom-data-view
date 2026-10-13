using CustomDataView.Api.Requests;
using CustomDataView.Core.Connections.Management;
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

            return Results.Created((string)null, new { id = service.Add(request.ToInput().ToDefinition()) });
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
        return endpoints;
    }
}
