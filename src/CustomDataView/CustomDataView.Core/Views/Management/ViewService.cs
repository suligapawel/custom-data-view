using CustomDataView.Core.Views.Definitions;

namespace CustomDataView.Core.Views.Management;

public sealed class ViewService : IViewService
{
    public Guid Add(ViewDefinition definition) => Guid.NewGuid();
}
