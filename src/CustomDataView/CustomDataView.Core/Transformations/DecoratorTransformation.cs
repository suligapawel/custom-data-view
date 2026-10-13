namespace CustomDataView.Core.Transformations;

public abstract class DecoratorTransformation : ITransformation
{
    private readonly ITransformation _inner;

    protected DecoratorTransformation(ITransformation inner, TransformationParameters parameters)
    {
        _inner = inner;
        Parameters = parameters;
    }

    public abstract string Key { get; }

    protected TransformationParameters Parameters { get; }

    public object Apply(object value)
    {
        return Transform(_inner.Apply(value));
    }

    protected abstract object Transform(object value);
}
