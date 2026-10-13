namespace CustomDataView.Core.Transformations.Catalog;

public interface ITransformationCatalog
{
    IReadOnlyList<ITransformation> GetAll();

    ITransformation Find(string key);
}
