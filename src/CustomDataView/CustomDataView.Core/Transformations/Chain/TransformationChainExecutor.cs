namespace CustomDataView.Core.Transformations.Chain;

public sealed class TransformationChainExecutor
{
    public const string TransformationFailedCode = "transformation_failed";

    private readonly TransformationChainBuilder _builder;

    public TransformationChainExecutor(TransformationChainBuilder builder)
    {
        _builder = builder;
    }

    public TransformationChainResult Execute(object value, IReadOnlyList<TransformationStep> steps)
    {
        try
        {
            return new TransformationChainResult(_builder.Build(steps).Apply(value));
        }
        catch (TransformationFailedException exception)
        {
            return new TransformationChainResult(null, new TransformationChainError(TransformationFailedCode, exception.Key, exception.Reason));
        }
    }
}
