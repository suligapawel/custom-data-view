namespace CustomDataView.Core.Transformations.Chain;

public sealed class TransformationChainExecutor
{
    private readonly TransformationChainBuilder _builder;

    public TransformationChainExecutor(TransformationChainBuilder builder)
    {
        _builder = builder;
    }

    public TransformationChainResult Execute(object value, IReadOnlyList<TransformationStep> steps)
    {
        return new TransformationChainResult(_builder.Build(steps).Apply(value));
    }
}
