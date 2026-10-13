using CustomDataView.Core.Transformations.Abstractions;

namespace CustomDataView.Core.Transformations.Catalog;

public sealed class TransformationCatalogService : ITransformationCatalogService
{
    private readonly IReadOnlyList<TransformationDto> _catalog;

    public TransformationCatalogService(IEnumerable<ITransformation> transformations)
    {
        _catalog = transformations
            .OrderBy(transformation => transformation.Key, StringComparer.Ordinal)
            .Select(transformation => new TransformationDto(
                transformation.Key,
                transformation.Name,
                transformation.Description,
                transformation.Parameters.Select(parameter => new TransformationParameterDto(parameter.Name, parameter.Type)).ToList()))
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<TransformationDto> GetAll()
    {
        return _catalog;
    }
}
