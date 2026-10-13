namespace CustomDataView.Core.Transformations.Truncate;

public sealed class TruncateTransformation : DecoratorTransformation
{
    private const int MinMaxLength = 1;

    private static readonly TransformationParameterDescriptor MaxLengthParameter = TruncateTransformationDefinition.MaxLengthParameter;

    public TruncateTransformation(ITransformation inner, TransformationParameters parameters)
        : base(inner, parameters)
    {
    }

    public override string Key => TruncateTransformationDefinition.TransformationKey;

    protected override object Transform(object value)
    {
        var maxLength = Parameters.GetInteger(Key, MaxLengthParameter.Name);
        if (maxLength < MinMaxLength)
        {
            throw new TransformationFailedException(Key, TransformationFailureReasons.InvalidParameter, MaxLengthParameter.Name);
        }

        var text = TransformationValue.ToText(Key, value);
        return text.Length > maxLength ? text.Substring(0, maxLength) : text;
    }
}
