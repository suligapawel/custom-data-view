namespace CustomDataView.Core.Transformations.Abstractions;

public abstract class Transformation : ITransformation
{
    public abstract string Key { get; }

    public abstract string Name { get; }

    public abstract string Description { get; }

    public virtual IReadOnlyList<TransformationParameterDescriptor> Parameters => [];

    public object Apply(object value, TransformationParameters parameters)
    {
        return value is null ? null : Transform(value, parameters);
    }

    protected abstract object Transform(object value, TransformationParameters parameters);
}
