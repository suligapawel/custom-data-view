using System.Text.Json;

namespace CustomDataView.Core.Views.Preview;

public interface IViewPreviewService
{
    Task<ViewPreview> Preview(Guid id, IReadOnlyDictionary<string, JsonElement> parameters, CancellationToken cancellationToken);
}
