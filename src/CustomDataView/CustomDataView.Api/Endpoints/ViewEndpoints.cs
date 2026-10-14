using CustomDataView.Api.Requests;
using CustomDataView.Api.Responses;
using CustomDataView.Core.Views.Management;
using CustomDataView.Core.Views.Preview;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CustomDataView.Api.Endpoints;

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
            endpoints.MapPost(ViewsUrl, (ViewRequest request, IViewService service) =>
            {
                var errors = request.Validate();
                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var id = service.Add(request.ToDefinition());
                return Results.Created($"{ViewsUrl}/{id}", new ViewCreatedResponse(id));
            });
            return endpoints;
        }

        private IEndpointRouteBuilder GetViews()
        {
            endpoints.MapGet(ViewsUrl, (IViewService service) => Results.Ok(service.GetAll()));
            return endpoints;
        }

        private IEndpointRouteBuilder GetView()
        {
            endpoints.MapGet(ViewUrl, (Guid id, IViewService service) => Results.Ok(service.Get(id)));
            return endpoints;
        }

        private IEndpointRouteBuilder UpdateView()
        {
            endpoints.MapPut(ViewUrl, (Guid id, ViewRequest request, IViewService service) =>
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

        private IEndpointRouteBuilder DeleteView()
        {
            endpoints.MapDelete(ViewUrl, (Guid id, IViewService service) =>
            {
                service.Delete(id);
                return Results.NoContent();
            });
            return endpoints;
        }

        private IEndpointRouteBuilder PreviewView()
        {
            endpoints.MapPost(
                $"{ViewUrl}/preview",
                async (Guid id, TilePreviewRequest request, IViewPreviewService service, CancellationToken cancellationToken)
                    => Results.Ok(await service.Preview(id, request.ToInput(), cancellationToken)));
            return endpoints;
        }

        private IEndpointRouteBuilder PreviewViewWithQuery()
        {
            endpoints.MapGet(
                $"{ViewUrl}/preview",
                async (Guid id, HttpContext httpContext, IViewPreviewService service, CancellationToken cancellationToken)
                    => Results.Ok(await service.Preview(
                        id,
                        TilePreviewRequest.ValuesOf(httpContext.Request.Query),
                        cancellationToken)));
            return endpoints;
        }
    }
}
