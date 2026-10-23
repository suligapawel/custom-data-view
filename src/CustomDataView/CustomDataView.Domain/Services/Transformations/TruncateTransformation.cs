using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Transformations;

public sealed class TruncateTransformation : Transformation
{
    private const int MinMaxLength = 1;

    private static readonly TransformationParameterDescriptor MaxLengthParameter =
        new("max_length", TransformationParameterTypes.Integer);

    public override string Key => "truncate";

    public override string Name => "Przycięcie tekstu";

    public override string Description => "Przycina tekst do maksymalnej liczby znaków.";

    public override TransformationParameterDescriptor Parameter => MaxLengthParameter;

    protected override object Transform(object value, string parameter)
    {
        var maxLength = RequireInteger(parameter);
        if (maxLength < MinMaxLength)
        {
            throw InvalidParameter();
        }

        var text = TransformationValue.ToText(Key, value);
        return text.Length > maxLength ? text.Substring(0, maxLength) : text;
    }
}
