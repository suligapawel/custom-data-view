namespace CustomDataView.Core.Transformations.Round;

public sealed class RoundTransformationDefinition : ITransformationDefinition
{
    public const string TransformationKey = "round";

    public string Key => TransformationKey;

    public ITransformation Create(ITransformation inner, TransformationParameters parameters)
    {
        return new RoundTransformation(inner, parameters);
    }
}
