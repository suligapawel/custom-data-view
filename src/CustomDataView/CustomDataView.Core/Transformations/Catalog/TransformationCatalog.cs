namespace CustomDataView.Core.Transformations.Catalog;

public sealed class TransformationCatalog
{
    private readonly Dictionary<string, ITransformationDefinition> _definitions;

    public TransformationCatalog(IEnumerable<ITransformationDefinition> definitions)
    {
        _definitions = definitions.ToDictionary(definition => definition.Key, StringComparer.Ordinal);
    }

    public ITransformationDefinition Find(string key)
    {
        return _definitions.GetValueOrDefault(key);
    }
}
