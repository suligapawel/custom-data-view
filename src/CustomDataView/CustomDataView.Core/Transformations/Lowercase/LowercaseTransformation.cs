namespace CustomDataView.Core.Transformations.Lowercase;

public sealed class LowercaseTransformation : DecoratorTransformation
{
    public LowercaseTransformation(ITransformation inner, TransformationParameters parameters)
        : base(inner, parameters)
    {
    }

    public override string Key => LowercaseTransformationDefinition.TransformationKey;

    protected override object Transform(object value)
    {
        return TransformationValue.ToText(Key, value).ToLowerInvariant();
    }
}
