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
            var definition = _catalog.Find(step.Key);
            chain = definition is null
                ? new UnknownTransformation(chain, step.Key)
                : definition.Create(chain, new TransformationParameters(step.Parameters));
        }

        return chain;
    }
}
