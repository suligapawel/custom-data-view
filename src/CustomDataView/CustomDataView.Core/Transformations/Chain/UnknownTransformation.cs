namespace CustomDataView.Core.Transformations.Chain;

public sealed class UnknownTransformation : ITransformation
{
    private readonly ITransformation _inner;

    public UnknownTransformation(ITransformation inner, string key)
    {
        _inner = inner;
        Key = key;
    }

    public string Key { get; }

    public object Apply(object value)
    {
        _inner.Apply(value);
        throw new TransformationFailedException(Key, TransformationFailureReasons.UnknownTransformation);
    }
}
