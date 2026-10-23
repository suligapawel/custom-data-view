using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services;

public interface ITransformationService
{
    TransformationResult Apply(object value, IReadOnlyList<TransformationStep> steps);
}
