using CustomDataView.Api.Requests;
using CustomDataView.Api.Responses;
using CustomDataView.Core.Views.Management;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CustomDataView.Api.Endpoints;

public static class ViewEndpoints
{
    private const string ViewsUrl = "/api/custom-data-view/views";

    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapViewEndpoints()
        {
            return endpoints
                .AddView();
        }

        private IEndpointRouteBuilder AddView()
        {
            endpoints.MapPost(ViewsUrl, (ViewRequest request, IViewService service) =>
            {
                var id = service.Add(request.ToDefinition());
                return Results.Created($"{ViewsUrl}/{id}", new ViewCreatedResponse(id));
            });
            return endpoints;
        }
    }
}
