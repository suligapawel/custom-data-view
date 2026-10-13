namespace CustomDataView.Core.Transformations.Prefix;

public sealed class PrefixTransformation : DecoratorTransformation
{
    private const string TextParameter = "text";

    public PrefixTransformation(ITransformation inner, TransformationParameters parameters)
        : base(inner, parameters)
    {
    }

    public override string Key => PrefixTransformationDefinition.TransformationKey;

    protected override object Transform(object value)
    {
        return Parameters.GetText(Key, TextParameter) + TransformationValue.ToText(Key, value);
    }
}
