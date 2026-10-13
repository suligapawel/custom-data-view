namespace CustomDataView.Core.Transformations.Round;

public sealed class RoundTransformation : DecoratorTransformation
{
    private static readonly TransformationParameterDescriptor DecimalsParameter = RoundTransformationDefinition.DecimalsParameter;

    private const int MinDecimals = 0;
    private const int MaxDecimals = 10;

    public RoundTransformation(ITransformation inner, TransformationParameters parameters)
        : base(inner, parameters)
    {
    }

    public override string Key => RoundTransformationDefinition.TransformationKey;

    protected override object Transform(object value)
    {
        var decimals = Parameters.GetInteger(Key, DecimalsParameter.Name);
        if (decimals < MinDecimals || decimals > MaxDecimals)
        {
            throw new TransformationFailedException(Key, TransformationFailureReasons.InvalidParameter, DecimalsParameter.Name);
        }

        return Math.Round(TransformationValue.ToNumber(Key, value), decimals, MidpointRounding.AwayFromZero);
    }
}
