namespace CustomDataView.Core.Transformations.Round;

public sealed class RoundTransformation : ITransformation
{
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
        var decimals = _parameters.GetInteger("decimals");
        return Math.Round(TransformationValue.ToNumber(Key, _inner.Apply(value)), decimals, MidpointRounding.AwayFromZero);
    }
}
