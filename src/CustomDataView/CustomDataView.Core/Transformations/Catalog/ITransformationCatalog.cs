namespace CustomDataView.Core.Transformations.Catalog;

public interface ITransformationCatalog
{
    IReadOnlyList<ITransformationDefinition> GetAll();

    ITransformationDefinition Find(string key);
}
