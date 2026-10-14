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

    public View Get(Guid id) => views.SingleOrDefault(view => view.Id == id);
}
