namespace CustomDataView.Core.Transformations.Catalog;

public interface ITransformationService
{
    IReadOnlyList<TransformationDto> GetAll();

    TransformationResult Apply(object value, IReadOnlyList<TransformationStep> steps);
}
