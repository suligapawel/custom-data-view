using CustomDataView.Core.Transformations.Catalog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CustomDataView.Api.Endpoints;

public static class TransformationEndpoints
{
    public static IEndpointRouteBuilder MapTransformationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/custom-data-view/transformations", (ITransformationService service) => service.GetAll());
        return endpoints;
    }
}
