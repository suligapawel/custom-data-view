namespace CustomDataView.Core.Transformations.Suffix;

public sealed class SuffixTransformation : DecoratorTransformation
{
    private static readonly TransformationParameterDescriptor TextParameter = SuffixTransformationDefinition.TextParameter;

    public SuffixTransformation(ITransformation inner, TransformationParameters parameters)
        : base(inner, parameters)
    {
    }

    public override string Key => SuffixTransformationDefinition.TransformationKey;

    protected override object Transform(object value)
    {
        return TransformationValue.ToText(Key, value) + Parameters.GetText(Key, TextParameter.Name);
    }
}
