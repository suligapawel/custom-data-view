namespace CustomDataView.Core.Transformations.Uppercase;

public sealed class UppercaseTransformation : DecoratorTransformation
{
    public UppercaseTransformation(ITransformation inner, TransformationParameters parameters)
        : base(inner, parameters)
    {
    }

    public override string Key => UppercaseTransformationDefinition.TransformationKey;

    protected override object Transform(object value)
    {
        return TransformationValue.ToText(Key, value).ToUpperInvariant();
    }
}
