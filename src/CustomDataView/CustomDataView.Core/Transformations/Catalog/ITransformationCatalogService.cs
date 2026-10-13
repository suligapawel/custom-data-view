namespace CustomDataView.Core.Transformations.Catalog;

public interface ITransformationCatalogService
{
    IReadOnlyList<TransformationDto> GetAll();
}
