using CustomDataView.Core.Views.Definitions;

namespace CustomDataView.Core.Views.Management;

public interface IViewService
{
    Guid Add(ViewDefinition definition);
}
