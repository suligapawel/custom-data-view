using CustomDataView.Api.Requests;
using CustomDataView.Api.Responses;
using CustomDataView.Core.Tiles.Management;
using CustomDataView.Core.Tiles.Preview;
using CustomDataView.Core.Tiles.Validation;
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
                .GetTile()
                .UpdateTile()
                .DeleteTile()
                .PreviewUnsavedTile()
                .PreviewSavedTile();
        }

        private IEndpointRouteBuilder AddTile()
        {
            endpoints.MapPost(TilesUrl, (TileRequest request, ITileService service) =>
            {
                var errors = request.Validate();
                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

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

        private IEndpointRouteBuilder UpdateTile()
        {
            endpoints.MapPut(TileUrl, (Guid id, TileRequest request, ITileService service) =>
            {
                var errors = request.Validate();
                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                service.Update(id, request.ToDefinition());
                return Results.NoContent();
            });
            return endpoints;
        }

        private IEndpointRouteBuilder DeleteTile()
        {
            endpoints.MapDelete(TileUrl, (Guid id, ITileService service) =>
            {
                service.Delete(id);
                return Results.NoContent();
            });
            return endpoints;
        }

        private IEndpointRouteBuilder PreviewUnsavedTile()
        {
            endpoints.MapPost(
                $"{TilesUrl}/preview",
                async (TileDraftPreviewRequest request, TileReferencesValidator referencesValidator,
                    ITilePreviewService service, CancellationToken cancellationToken) =>
                {
                    var errors = request.Validate(referencesValidator);
                    if (errors.Count > 0)
                    {
                        return Results.ValidationProblem(errors);
                    }

                    return Results.Ok(await service.Preview(
                        request.Tile.ToDefinition(),
                        TilePreviewRequest.ValuesOf(request.Parameters),
                        cancellationToken));
                });
            return endpoints;
        }

        private IEndpointRouteBuilder PreviewSavedTile()
        {
            endpoints.MapPost(
                $"{TileUrl}/preview",
                async (Guid id, TilePreviewRequest request, ITilePreviewService service,
                        CancellationToken cancellationToken)
                    => Results.Ok(await service.Preview(id, request.ToInput(), cancellationToken)));
            return endpoints;
        }
    }
}
