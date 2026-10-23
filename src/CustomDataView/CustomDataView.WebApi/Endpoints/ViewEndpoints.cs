using CustomDataView.Application.Views.Commands;
using CustomDataView.Application.Views.Dtos;
using CustomDataView.Application.Views.Preview;
using CustomDataView.Application.Views.Queries;
using CustomDataView.WebApi.Requests;
using CustomDataView.WebApi.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace CustomDataView.WebApi.Endpoints;

public static class ViewEndpoints
{
    private const string ViewsUrl = "/api/custom-data-view/views";
    private const string ViewUrl = $"{ViewsUrl}/{{id:guid}}";

    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapViewEndpoints()
        {
            return endpoints
                .AddView()
                .GetViews()
                .GetView()
                .UpdateView()
                .DeleteView()
                .PreviewView()
                .PreviewViewWithQuery();
        }

        private IEndpointRouteBuilder AddView()
        {
            endpoints.MapPost(
                ViewsUrl,
                async (ViewRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
                {
                    var errors = request.Validate();
                    if (errors.Count > 0)
                    {
                        return Results.ValidationProblem(errors);
                    }

                    var id = await bus.InvokeAsync<Guid>(new AddView(request.ToDefinition()), cancellationToken);
                    return Results.Created($"{ViewsUrl}/{id}", new ViewCreatedResponse(id));
                });
            return endpoints;
        }

        private IEndpointRouteBuilder GetViews()
        {
            endpoints.MapGet(
                ViewsUrl,
                async (IMessageBus bus, CancellationToken cancellationToken)
                    => Results.Ok(await bus.InvokeAsync<IReadOnlyList<ViewSummaryDto>>(new GetViews(), cancellationToken)));
            return endpoints;
        }

        private IEndpointRouteBuilder GetView()
        {
            endpoints.MapGet(
                ViewUrl,
                async (Guid id, IMessageBus bus, CancellationToken cancellationToken)
                    => Results.Ok(await bus.InvokeAsync<ViewDto>(new GetView(id), cancellationToken)));
            return endpoints;
        }

        private IEndpointRouteBuilder UpdateView()
        {
            endpoints.MapPut(
                ViewUrl,
                async (Guid id, ViewRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
                {
                    var errors = request.Validate();
                    if (errors.Count > 0)
                    {
                        return Results.ValidationProblem(errors);
                    }

                    await bus.InvokeAsync(new UpdateView(id, request.ToDefinition()), cancellationToken);
                    return Results.NoContent();
                });
            return endpoints;
        }

        private IEndpointRouteBuilder DeleteView()
        {
            endpoints.MapDelete(
                ViewUrl,
                async (Guid id, IMessageBus bus, CancellationToken cancellationToken) =>
                {
                    await bus.InvokeAsync(new DeleteView(id), cancellationToken);
                    return Results.NoContent();
                });
            return endpoints;
        }

        private IEndpointRouteBuilder PreviewView()
        {
            endpoints.MapPost(
                $"{ViewUrl}/preview",
                async (Guid id, TilePreviewRequest request, IMessageBus bus, CancellationToken cancellationToken)
                    => Results.Ok(await bus.InvokeAsync<ViewPreview>(new PreviewView(id, request.ToInput()), cancellationToken)));
            return endpoints;
        }

        private IEndpointRouteBuilder PreviewViewWithQuery()
        {
            endpoints.MapGet(
                $"{ViewUrl}/preview",
                async (Guid id, HttpContext httpContext, IMessageBus bus, CancellationToken cancellationToken) =>
                {
                    var query = new PreviewView(id, TilePreviewRequest.ValuesOf(httpContext.Request.Query));
                    return Results.Ok(await bus.InvokeAsync<ViewPreview>(query, cancellationToken));
                });
            return endpoints;
        }
    }
}
