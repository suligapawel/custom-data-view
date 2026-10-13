using CustomDataView.Core.Transformations.Catalog;

namespace CustomDataView.Core.Transformations.Chain;

public sealed class TransformationChainExecutor
{
    public const string TransformationFailedCode = "transformation_failed";

    private readonly ITransformationCatalog _catalog;

    public TransformationChainExecutor(ITransformationCatalog catalog)
    {
        _catalog = catalog;
    }

    public TransformationChainResult Execute(object value, IReadOnlyList<TransformationStep> steps)
    {
        try
        {
            foreach (var step in steps)
            {
                var transformation = _catalog.Find(step.Key)
                    ?? throw new TransformationFailedException(step.Key, TransformationFailureReasons.UnknownTransformation);
                value = transformation.Apply(value, new TransformationParameters(step.Parameters));
            }

            return new TransformationChainResult(value);
        }
        catch (TransformationFailedException exception)
        {
            return new TransformationChainResult(null, new TransformationChainError(TransformationFailedCode, exception.Key, exception.Reason));
        }
    }
}
