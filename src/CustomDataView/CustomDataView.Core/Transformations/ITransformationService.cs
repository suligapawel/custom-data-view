namespace CustomDataView.Core.Transformations;

public interface ITransformationService
{
    TransformationResult Apply(object value, IReadOnlyList<TransformationStep> steps);
}
