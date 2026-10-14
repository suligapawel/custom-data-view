using CustomDataView.Core.Tiles.Persistence;
using CustomDataView.Core.Views.Definitions;
using CustomDataView.Core.Views.Persistence;

namespace CustomDataView.Core.Views.Management;

public sealed class ViewService(IViewRepository repository, ITileRepository tiles) : IViewService
{
    public Guid Add(ViewDefinition definition)
    {
        var view = new View(Guid.NewGuid(), definition);
        repository.Add(view);
        return view.Id;
    }

    public ViewDto Get(Guid id)
    {
        var view = repository.Get(id);
        var names = tiles.GetReferences(view.Definition.Tiles.Select(tile => tile.TileId).ToList())
            .ToDictionary(tile => tile.Id, tile => tile.Name);
        return new ViewDto(
            view.Id,
            view.Definition.Name,
            view.Definition.Tiles
                .Select(tile => new ViewTileDto(tile.TileId, names[tile.TileId], tile.Position, tile.CssClass))
                .ToList());
    }
}
