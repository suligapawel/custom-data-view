namespace CustomDataView.Core.Transformations.Catalog;

public interface ITransformationService
{
    IReadOnlyList<TransformationDto> GetAll();

    ITransformation Find(string key);
}
