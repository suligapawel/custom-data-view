using CustomDataView.Application.Combinations.Dtos;
using CustomDataView.Application.Combinations.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace CustomDataView.WebApi.Endpoints;

public static class CombinationEndpoints
{
    public static IEndpointRouteBuilder MapCombinationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/api/custom-data-view/combinations",
            (IMessageBus bus, CancellationToken cancellationToken)
                => bus.InvokeAsync<IReadOnlyList<CombinationDto>>(new GetCombinations(), cancellationToken));
        return endpoints;
    }
}
