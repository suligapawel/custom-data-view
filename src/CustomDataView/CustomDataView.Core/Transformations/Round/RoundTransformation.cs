namespace CustomDataView.Core.Transformations.Round;

public sealed class RoundTransformation : DecoratorTransformation
{
    private const string DecimalsParameter = "decimals";
    private const int MinDecimals = 0;
    private const int MaxDecimals = 10;

    public RoundTransformation(ITransformation inner, TransformationParameters parameters)
        : base(inner, parameters)
    {
    }

    public override string Key => "round";

    protected override object Transform(object value)
    {
        var decimals = Parameters.GetInteger(Key, DecimalsParameter);
        if (decimals < MinDecimals || decimals > MaxDecimals)
        {
            throw new TransformationFailedException(Key, TransformationFailureReasons.InvalidParameter, DecimalsParameter);
        }

        return Math.Round(TransformationValue.ToNumber(Key, value), decimals, MidpointRounding.AwayFromZero);
    }
}
