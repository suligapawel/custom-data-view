namespace CustomDataView.Core.Transformations.Catalog;

public sealed class TransformationCatalogService : ITransformationCatalogService
{
    private readonly ITransformationCatalog _catalog;

    public TransformationCatalogService(ITransformationCatalog catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyList<TransformationDto> GetAll()
    {
        return _catalog.GetAll()
            .Select(transformation => new TransformationDto(
                transformation.Key,
                transformation.Name,
                transformation.Description,
                transformation.Parameters.Select(parameter => new TransformationParameterDto(parameter.Name, parameter.Type)).ToList()))
            .ToList();
    }
}
