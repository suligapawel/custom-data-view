namespace CustomDataView.Core.Transformations.Prefix;

public sealed class PrefixTransformationDefinition : ITransformationDefinition
{
    public const string TransformationKey = "prefix";

    public string Key => TransformationKey;

    public ITransformation Create(ITransformation inner, TransformationParameters parameters)
    {
        return new PrefixTransformation(inner, parameters);
    }
}
