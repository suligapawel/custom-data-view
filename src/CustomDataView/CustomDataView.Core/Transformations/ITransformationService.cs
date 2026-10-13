namespace CustomDataView.Core.Transformations;

public interface ITransformationService
{
    IReadOnlyList<TransformationDto> GetAll();

    ITransformation Find(string key);
}
