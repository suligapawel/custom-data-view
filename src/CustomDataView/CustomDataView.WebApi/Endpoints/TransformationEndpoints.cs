using CustomDataView.Application.Transformations.Dtos;
using CustomDataView.Application.Transformations.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace CustomDataView.WebApi.Endpoints;

public static class TransformationEndpoints
{
    public static IEndpointRouteBuilder MapTransformationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/api/custom-data-view/transformations",
            (IMessageBus bus, CancellationToken cancellationToken)
                => bus.InvokeAsync<IReadOnlyList<TransformationDto>>(new GetTransformations(), cancellationToken));
        return endpoints;
    }
}
