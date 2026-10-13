namespace CustomDataView.Core.Transformations.Truncate;

public sealed class TruncateTransformation : DecoratorTransformation
{
    private const string MaxLengthParameter = "max_length";
    private const int MinMaxLength = 1;

    public TruncateTransformation(ITransformation inner, TransformationParameters parameters)
        : base(inner, parameters)
    {
    }

    public override string Key => "truncate";

    protected override object Transform(object value)
    {
        var maxLength = Parameters.GetInteger(Key, MaxLengthParameter);
        if (maxLength < MinMaxLength)
        {
            throw new TransformationFailedException(Key, TransformationFailureReasons.InvalidParameter, MaxLengthParameter);
        }

        var text = TransformationValue.ToText(Key, value);
        return text.Length > maxLength ? text.Substring(0, maxLength) : text;
    }
}
