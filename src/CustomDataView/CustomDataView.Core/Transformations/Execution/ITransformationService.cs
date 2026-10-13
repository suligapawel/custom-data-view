namespace CustomDataView.Core.Transformations.Execution;

public interface ITransformationService
{
    TransformationResult Apply(object value, IReadOnlyList<TransformationStep> steps);
}
