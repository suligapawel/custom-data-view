namespace CustomDataView.Core.Transformations.Catalog;

public sealed class TransformationCatalog : ITransformationCatalog
{
    private readonly IReadOnlyList<ITransformationDefinition> _sortedDefinitions;
    private readonly Dictionary<string, ITransformationDefinition> _definitions;

    public TransformationCatalog(IEnumerable<ITransformationDefinition> definitions)
    {
        _sortedDefinitions = definitions.OrderBy(definition => definition.Key, StringComparer.Ordinal).ToList().AsReadOnly();
        _definitions = _sortedDefinitions.ToDictionary(definition => definition.Key, StringComparer.Ordinal);
    }

    public IReadOnlyList<ITransformationDefinition> GetAll()
    {
        return _sortedDefinitions;
    }

    public ITransformationDefinition Find(string key)
    {
        return _definitions.GetValueOrDefault(key);
    }
}
