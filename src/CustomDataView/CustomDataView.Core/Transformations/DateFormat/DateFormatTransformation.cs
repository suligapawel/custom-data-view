using System.Globalization;

namespace CustomDataView.Core.Transformations.DateFormat;

public sealed class DateFormatTransformation : DecoratorTransformation
{
    private static readonly TransformationParameterDescriptor PatternParameter = DateFormatTransformationDefinition.PatternParameter;

    private static readonly string[] IsoFormats = ["yyyy-MM-ddTHH:mm:sszzz", "yyyy-MM-dd"];

    public DateFormatTransformation(ITransformation inner, TransformationParameters parameters)
        : base(inner, parameters)
    {
    }

    public override string Key => DateFormatTransformationDefinition.TransformationKey;

    protected override object Transform(object value)
    {
        var pattern = Parameters.GetText(Key, PatternParameter.Name);
        if (pattern.Length == 0)
        {
            throw new TransformationFailedException(Key, TransformationFailureReasons.InvalidParameter, PatternParameter.Name);
        }

        if (value is not string text || !DateTimeOffset.TryParseExact(
                text,
                IsoFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var date))
        {
            throw new TransformationFailedException(Key, TransformationFailureReasons.InvalidValue);
        }

        return date.ToString(pattern, CultureInfo.InvariantCulture);
    }
}
