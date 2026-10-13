using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CustomDataView.Api.Endpoints;

public static class ConnectionEndpoints
{
    public static IEndpointRouteBuilder MapConnectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/custom-data-view/connections", () => Results.Created((string)null, new { id = Guid.NewGuid() }));
        return endpoints;
    }
}
