using CustomDataView.Core.Transformations.Catalog;

namespace CustomDataView.Core.Transformations.Chain;

public sealed class TransformationChainExecutor
{
    public const string TransformationFailedCode = "transformation_failed";

    private readonly ITransformationService _service;

    public TransformationChainExecutor(ITransformationService service)
    {
        _service = service;
    }

    public TransformationChainResult Execute(object value, IReadOnlyList<TransformationStep> steps)
    {
        try
        {
            foreach (var step in steps)
            {
                var transformation = _service.Find(step.Key)
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
