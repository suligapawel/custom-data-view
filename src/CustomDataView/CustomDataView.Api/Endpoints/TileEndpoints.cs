using CustomDataView.Api.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CustomDataView.Api.Endpoints;

public static class TileEndpoints
{
    private const string TilesUrl = "/api/custom-data-view/tiles";

    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapTileEndpoints()
        {
            return endpoints
                .AddTile();
        }

        private IEndpointRouteBuilder AddTile()
        {
            endpoints.MapPost(TilesUrl, () =>
            {
                var id = Guid.NewGuid();
                return Results.Created($"{TilesUrl}/{id}", new TileCreatedResponse(id));
            });
            return endpoints;
        }
    }
}
