namespace CustomDataView.Core.Transformations.Catalog;

public sealed class TransformationService : ITransformationService
{
    private readonly Dictionary<string, ITransformation> _transformations;
    private readonly IReadOnlyList<TransformationDto> _catalog;

    public TransformationService(IEnumerable<ITransformation> transformations)
    {
        _transformations = transformations.ToDictionary(transformation => transformation.Key, StringComparer.Ordinal);
        _catalog = _transformations.Values
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

    public ITransformation Find(string key)
    {
        return _transformations.GetValueOrDefault(key);
    }
}
