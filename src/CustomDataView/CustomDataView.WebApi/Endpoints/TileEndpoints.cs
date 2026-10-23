using CustomDataView.Application.Tiles.Commands;
using CustomDataView.Application.Tiles.Dtos;
using CustomDataView.Application.Tiles.Preview;
using CustomDataView.Application.Tiles.Queries;
using CustomDataView.Application.Tiles.Validation;
using CustomDataView.WebApi.Requests;
using CustomDataView.WebApi.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace CustomDataView.WebApi.Endpoints;

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
                .PreviewSavedTile()
                .PreviewSavedTileWithQuery();
        }

        private IEndpointRouteBuilder AddTile()
        {
            endpoints.MapPost(
                TilesUrl,
                async (TileRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
                {
                    var errors = request.Validate();
                    if (errors.Count > 0)
                    {
                        return Results.ValidationProblem(errors);
                    }

                    var id = await bus.InvokeAsync<Guid>(new AddTile(request.ToDefinition()), cancellationToken);
                    return Results.Created($"{TilesUrl}/{id}", new TileCreatedResponse(id));
                });
            return endpoints;
        }

        private IEndpointRouteBuilder GetTiles()
        {
            endpoints.MapGet(
                TilesUrl,
                async (IMessageBus bus, CancellationToken cancellationToken)
                    => Results.Ok(await bus.InvokeAsync<IReadOnlyList<TileSummaryDto>>(new GetTiles(), cancellationToken)));
            return endpoints;
        }

        private IEndpointRouteBuilder GetTile()
        {
            endpoints.MapGet(
                TileUrl,
                async (Guid id, IMessageBus bus, CancellationToken cancellationToken)
                    => Results.Ok(await bus.InvokeAsync<TileDto>(new GetTile(id), cancellationToken)));
            return endpoints;
        }

        private IEndpointRouteBuilder UpdateTile()
        {
            endpoints.MapPut(
                TileUrl,
                async (Guid id, TileRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
                {
                    var errors = request.Validate();
                    if (errors.Count > 0)
                    {
                        return Results.ValidationProblem(errors);
                    }

                    await bus.InvokeAsync(new UpdateTile(id, request.ToDefinition()), cancellationToken);
                    return Results.NoContent();
                });
            return endpoints;
        }

        private IEndpointRouteBuilder DeleteTile()
        {
            endpoints.MapDelete(
                TileUrl,
                async (Guid id, IMessageBus bus, CancellationToken cancellationToken) =>
                {
                    await bus.InvokeAsync(new DeleteTile(id), cancellationToken);
                    return Results.NoContent();
                });
            return endpoints;
        }

        private IEndpointRouteBuilder PreviewUnsavedTile()
        {
            endpoints.MapPost(
                $"{TilesUrl}/preview",
                async (TileDraftPreviewRequest request, TileReferencesValidator referencesValidator, IMessageBus bus,
                    CancellationToken cancellationToken) =>
                {
                    var errors = request.Validate(referencesValidator);
                    if (errors.Count > 0)
                    {
                        return Results.ValidationProblem(errors);
                    }

                    var query = new PreviewTileDraft(request.Tile.ToDefinition(), TilePreviewRequest.ValuesOf(request.Parameters));
                    return Results.Ok(await bus.InvokeAsync<TilePreview>(query, cancellationToken));
                });
            return endpoints;
        }

        private IEndpointRouteBuilder PreviewSavedTile()
        {
            endpoints.MapPost(
                $"{TileUrl}/preview",
                async (Guid id, TilePreviewRequest request, IMessageBus bus, CancellationToken cancellationToken)
                    => Results.Ok(await bus.InvokeAsync<TilePreview>(new PreviewTile(id, request.ToInput()), cancellationToken)));
            return endpoints;
        }

        private IEndpointRouteBuilder PreviewSavedTileWithQuery()
        {
            endpoints.MapGet(
                $"{TileUrl}/preview",
                async (Guid id, HttpContext httpContext, IMessageBus bus, CancellationToken cancellationToken) =>
                {
                    var query = new PreviewTile(id, TilePreviewRequest.ValuesOf(httpContext.Request.Query));
                    return Results.Ok(await bus.InvokeAsync<TilePreview>(query, cancellationToken));
                });
            return endpoints;
        }
    }
}
