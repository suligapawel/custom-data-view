using CustomDataView.Domain.Entities;

namespace CustomDataView.Domain.Repositories;

public interface IViewRepository
{
    void Add(View view);
    View Get(Guid id);
    IReadOnlyList<ViewSummary> GetAll();
    void Update(View view);
    void Delete(Guid id);
}
