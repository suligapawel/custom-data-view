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
        endpoints.MapPost(ConnectionsUrl, (IConnectionService service) =>
            Results.Created((string)null, new { id = service.Add() }));
        endpoints.MapGet($"{ConnectionsUrl}/{{id:guid}}", (Guid id, IConnectionService service) =>
            Results.Ok(service.Get(id)));
        return endpoints;
    }
}
