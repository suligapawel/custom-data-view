namespace CustomDataView.Core.Transformations.Implementations;

public sealed class RoundTransformation : Transformation
{
    private const int MinDecimals = 0;
    private const int MaxDecimals = 10;

    private static readonly TransformationParameterDescriptor DecimalsParameter =
        new("decimals", TransformationParameterTypes.Integer);

    public override string Key => "round";

    public override string Name => "Zaokrąglenie";

    public override string Description => "Zaokrągla liczbę do podanej liczby miejsc po przecinku (0-10).";

    public override IReadOnlyList<TransformationParameterDescriptor> Parameters { get; } = [DecimalsParameter];

    protected override object Transform(object value, TransformationParameters parameters)
    {
        var decimals = parameters.GetInteger(Key, DecimalsParameter.Name);
        if (decimals < MinDecimals || decimals > MaxDecimals)
        {
            throw new TransformationFailedException(Key, TransformationFailureReasons.InvalidParameter, DecimalsParameter.Name);
        }

        return Math.Round(TransformationValue.ToNumber(Key, value), decimals, MidpointRounding.AwayFromZero);
    }
}
