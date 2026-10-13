using CustomDataView.Core.Transformations.Catalog;

namespace CustomDataView.Core.Transformations.Chain;

public sealed class TransformationChainBuilder
{
    private readonly TransformationCatalog _catalog;

    public TransformationChainBuilder(TransformationCatalog catalog)
    {
        _catalog = catalog;
    }

    public ITransformation Build(IReadOnlyList<TransformationStep> steps)
    {
        ITransformation chain = new IdentityTransformation();
        foreach (var step in steps)
        {
            chain = _catalog.Find(step.Key).Create(chain, new TransformationParameters(step.Parameters));
        }

        return chain;
    }
}
