using CustomDataView.Core.Transformations.Abstractions;

namespace CustomDataView.Core.Transformations.Implementations;

public sealed class TruncateTransformation : Transformation
{
    private const int MinMaxLength = 1;

    private static readonly TransformationParameterDescriptor MaxLengthParameter =
        new("max_length", TransformationParameterTypes.Integer);

    public override string Key => "truncate";

    public override string Name => "Przycięcie tekstu";

    public override string Description => "Przycina tekst do maksymalnej liczby znaków.";

    public override IReadOnlyList<TransformationParameterDescriptor> Parameters { get; } = [MaxLengthParameter];

    protected override object Transform(object value, TransformationParameters parameters)
    {
        var maxLength = parameters.GetInteger(Key, MaxLengthParameter.Name);
        if (maxLength < MinMaxLength)
        {
            throw new TransformationFailedException(Key, TransformationFailureReasons.InvalidParameter, MaxLengthParameter.Name);
        }

        var text = TransformationValue.ToText(Key, value);
        return text.Length > maxLength ? text.Substring(0, maxLength) : text;
    }
}
