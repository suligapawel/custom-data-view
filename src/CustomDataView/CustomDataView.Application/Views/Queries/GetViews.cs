using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Views.Dtos;
using CustomDataView.Domain.Repositories;

namespace CustomDataView.Application.Views.Queries;

public sealed record GetViews : IQuery<IReadOnlyList<ViewSummaryDto>>;

public sealed class GetViewsHandler(IViewRepository repository)
{
    public IReadOnlyList<ViewSummaryDto> Handle(GetViews query)
        => repository.GetAll().Select(view => new ViewSummaryDto(view.Id, view.Name, view.TileCount)).ToList();
}
