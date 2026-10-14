using CustomDataView.Core.Views.Definitions;
using CustomDataView.Core.Views.Persistence;

namespace CustomDataView.Api.Tests;

public sealed class TestViewRepository : IViewRepository
{
    private readonly List<View> views = [];

    public void Add(View view)
    {
        views.Add(view);
    }

    public void RemoveTile(Guid tileId)
    {
        for (var index = 0; index < views.Count; index++)
        {
            var view = views[index];
            views[index] = view with
            {
                Definition = view.Definition with
                {
                    Tiles = view.Definition.Tiles.Where(tile => tile.TileId != tileId).ToList()
                }
            };
        }
    }

    public View Get(Guid id) => views.SingleOrDefault(view => view.Id == id);

    public IReadOnlyList<ViewSummary> GetAll() => views.Select(view => new ViewSummary(view.Id, view.Definition.Name)).ToList();
}
