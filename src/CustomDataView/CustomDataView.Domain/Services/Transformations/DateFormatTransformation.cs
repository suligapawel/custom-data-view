using System.Globalization;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Exceptions;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Transformations;

public sealed class DateFormatTransformation : Transformation
{
    private static readonly TransformationParameterDescriptor PatternParameter =
        new("pattern", TransformationParameterTypes.String);

    private static readonly string[] IsoFormats = ["yyyy-MM-ddTHH:mm:sszzz", "yyyy-MM-dd"];

    public override string Key => "date_format";

    public override string Name => "Formatowanie daty";

    public override string Description => "Formatuje datę ISO 8601 według wzorca w kulturze niezmiennej.";

    public override IReadOnlyList<TransformationParameterDescriptor> Parameters { get; } = [PatternParameter];

    protected override object Transform(object value, TransformationParameters parameters)
    {
        var pattern = parameters.GetText(Key, PatternParameter.Name);
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
