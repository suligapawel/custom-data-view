namespace CustomDataView.Core.Transformations.Uppercase;

public sealed class UppercaseTransformationDefinition : ITransformationDefinition
{
    public const string TransformationKey = "uppercase";

    public string Key => TransformationKey;

    public ITransformation Create(ITransformation inner, TransformationParameters parameters)
    {
        return new UppercaseTransformation(inner, parameters);
    }
}
