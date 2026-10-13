using CustomDataView.Api.Requests;
using CustomDataView.Api.Responses;
using CustomDataView.Core.Tiles.Management;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CustomDataView.Api.Endpoints;

public static class TileEndpoints
{
    private const string TilesUrl = "/api/custom-data-view/tiles";
    private const string TileUrl = $"{TilesUrl}/{{id:guid}}";

    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapTileEndpoints()
        {
            return endpoints
                .AddTile()
                .GetTiles()
                .GetTile();
        }

        private IEndpointRouteBuilder AddTile()
        {
            endpoints.MapPost(TilesUrl, (TileRequest request, ITileService service) =>
            {
                var id = service.Add(request.ToDefinition());
                return Results.Created($"{TilesUrl}/{id}", new TileCreatedResponse(id));
            });
            return endpoints;
        }

        private IEndpointRouteBuilder GetTiles()
        {
            endpoints.MapGet(TilesUrl, (ITileService service) => Results.Ok(service.GetAll()));
            return endpoints;
        }

        private IEndpointRouteBuilder GetTile()
        {
            endpoints.MapGet(TileUrl, (Guid id, ITileService service) => Results.Ok(service.Get(id)));
            return endpoints;
        }
    }
}
