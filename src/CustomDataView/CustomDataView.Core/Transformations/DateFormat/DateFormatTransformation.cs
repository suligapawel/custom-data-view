using System.Globalization;

namespace CustomDataView.Core.Transformations.DateFormat;

public sealed class DateFormatTransformation : ITransformation
{
    private const string PatternParameter = "pattern";

    private static readonly string[] IsoFormats = ["yyyy-MM-ddTHH:mm:sszzz", "yyyy-MM-dd"];

    private readonly ITransformation _inner;
    private readonly TransformationParameters _parameters;

    public DateFormatTransformation(ITransformation inner, TransformationParameters parameters)
    {
        _inner = inner;
        _parameters = parameters;
    }

    public string Key => "date_format";

    public object Apply(object value)
    {
        var innerValue = _inner.Apply(value);
        var pattern = _parameters.GetText(Key, PatternParameter);
        if (!DateTimeOffset.TryParseExact(
                (string)innerValue,
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
