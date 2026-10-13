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
            .Select(definition => new TransformationDto(
                definition.Key,
                definition.Name,
                definition.Description,
                definition.Parameters.Select(parameter => new TransformationParameterDto(parameter.Name, parameter.Type)).ToList()))
            .ToList();
    }
}
