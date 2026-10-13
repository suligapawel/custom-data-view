namespace CustomDataView.Core.Transformations.Round;

public sealed class RoundTransformation : ITransformation
{
    private const string DecimalsParameter = "decimals";
    private const int MinDecimals = 0;
    private const int MaxDecimals = 10;

    private readonly ITransformation _inner;
    private readonly TransformationParameters _parameters;

    public RoundTransformation(ITransformation inner, TransformationParameters parameters)
    {
        _inner = inner;
        _parameters = parameters;
    }

    public string Key => "round";

    public object Apply(object value)
    {
        var innerValue = _inner.Apply(value);
        var decimals = _parameters.GetInteger(Key, DecimalsParameter);
        if (decimals < MinDecimals || decimals > MaxDecimals)
        {
            throw new TransformationFailedException(Key, TransformationFailureReasons.InvalidParameter, DecimalsParameter);
        }

        return Math.Round(TransformationValue.ToNumber(Key, innerValue), decimals, MidpointRounding.AwayFromZero);
    }
}
