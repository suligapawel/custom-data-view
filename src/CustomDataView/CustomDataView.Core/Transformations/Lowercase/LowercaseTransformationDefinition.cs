namespace CustomDataView.Core.Transformations.Lowercase;

public sealed class LowercaseTransformationDefinition : ITransformationDefinition
{
    public const string TransformationKey = "lowercase";

    public string Key => TransformationKey;

    public ITransformation Create(ITransformation inner, TransformationParameters parameters)
    {
        return new LowercaseTransformation(inner, parameters);
    }
}
