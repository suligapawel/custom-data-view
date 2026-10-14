using CustomDataView.Core.Views.Definitions;

namespace CustomDataView.Core.Views.Management;

public interface IViewService
{
    Guid Add(ViewDefinition definition);
    IReadOnlyList<ViewSummaryDto> GetAll();
    ViewDto Get(Guid id);
    void Update(Guid id, ViewDefinition definition);
    void Delete(Guid id);
}
