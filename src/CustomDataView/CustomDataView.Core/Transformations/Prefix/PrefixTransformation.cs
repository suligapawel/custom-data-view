namespace CustomDataView.Core.Transformations.Prefix;

public sealed class PrefixTransformation : DecoratorTransformation
{
    private static readonly TransformationParameterDescriptor TextParameter = PrefixTransformationDefinition.TextParameter;

    public PrefixTransformation(ITransformation inner, TransformationParameters parameters)
        : base(inner, parameters)
    {
    }

    public override string Key => PrefixTransformationDefinition.TransformationKey;

    protected override object Transform(object value)
    {
        return Parameters.GetText(Key, TextParameter.Name) + TransformationValue.ToText(Key, value);
    }
}
