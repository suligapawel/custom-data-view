using System.Globalization;

namespace CustomDataView.Core.Transformations.DateFormat;

public sealed class DateFormatTransformation : DecoratorTransformation
{
    private const string PatternParameter = "pattern";

    private static readonly string[] IsoFormats = ["yyyy-MM-ddTHH:mm:sszzz", "yyyy-MM-dd"];

    public DateFormatTransformation(ITransformation inner, TransformationParameters parameters)
        : base(inner, parameters)
    {
    }

    public override string Key => "date_format";

    protected override object Transform(object value)
    {
        var pattern = Parameters.GetText(Key, PatternParameter);
        if (pattern.Length == 0)
        {
            throw new TransformationFailedException(Key, TransformationFailureReasons.InvalidParameter, PatternParameter);
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
