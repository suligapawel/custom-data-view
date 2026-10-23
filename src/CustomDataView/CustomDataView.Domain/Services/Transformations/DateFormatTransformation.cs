using System.Globalization;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Exceptions;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Transformations;

public sealed class DateFormatTransformation : Transformation
{
    private static readonly TransformationParameterDescriptor PatternParameter =
        new("pattern", TransformationParameterTypes.String);

    public override string Key => "date_format";

    public override string Name => "Formatowanie daty";

    public override string Description => "Formatuje datę ISO 8601 według wzorca w kulturze niezmiennej.";

    public override TransformationParameterDescriptor Parameter => PatternParameter;

    protected override object Transform(object value, string parameter)
    {
        var pattern = RequireText(parameter);
        if (pattern.Length == 0)
        {
            throw InvalidParameter();
        }

        if (value is not string text || !DateTimeOffset.TryParse(text, out var date))
        {
            throw new TransformationFailedException(Key, TransformationFailureReasons.InvalidValue);
        }

        return date.ToString(pattern, CultureInfo.InvariantCulture);
    }
}
