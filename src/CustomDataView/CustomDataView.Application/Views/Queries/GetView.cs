using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Exceptions;
using CustomDataView.Application.Views.Dtos;
using CustomDataView.Domain.Repositories;

namespace CustomDataView.Application.Views.Queries;

public sealed record GetView(Guid Id) : IQuery<ViewDto>;

public sealed class GetViewHandler(IViewRepository repository, ITileRepository tiles)
{
    public ViewDto Handle(GetView query)
    {
        var view = repository.Get(query.Id) ?? throw new NotFoundException(query.Id);
        var names = tiles.GetReferences(view.Definition.Tiles.Select(tile => tile.TileId).ToList())
            .ToDictionary(tile => tile.Id, tile => tile.Name);
        return new ViewDto(
            view.Id,
            view.Definition.Name,
            view.Definition.Tiles
                .OrderBy(tile => tile.Position)
                .Select(tile => new ViewTileDto(tile.TileId, names[tile.TileId], tile.Position, tile.CssClass))
                .ToList());
    }
}
