using CustomDataView.Core.Views.Definitions;

namespace CustomDataView.Core.Views.Persistence;

public interface IViewRepository
{
    void Add(View view);
    View Get(Guid id);
    IReadOnlyList<ViewSummary> GetAll();
}
