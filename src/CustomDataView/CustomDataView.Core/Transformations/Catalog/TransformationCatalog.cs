namespace CustomDataView.Core.Transformations.Catalog;

public sealed class TransformationCatalog : ITransformationCatalog
{
    private readonly IReadOnlyList<ITransformation> _sortedTransformations;
    private readonly Dictionary<string, ITransformation> _transformations;

    public TransformationCatalog(IEnumerable<ITransformation> transformations)
    {
        _sortedTransformations = transformations.OrderBy(transformation => transformation.Key, StringComparer.Ordinal).ToList().AsReadOnly();
        _transformations = _sortedTransformations.ToDictionary(transformation => transformation.Key, StringComparer.Ordinal);
    }

    public IReadOnlyList<ITransformation> GetAll()
    {
        return _sortedTransformations;
    }

    public ITransformation Find(string key)
    {
        return _transformations.GetValueOrDefault(key);
    }
}
