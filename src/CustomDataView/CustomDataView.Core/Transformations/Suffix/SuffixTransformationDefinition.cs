namespace CustomDataView.Core.Transformations.Suffix;

public sealed class SuffixTransformationDefinition : ITransformationDefinition
{
    public const string TransformationKey = "suffix";

    public string Key => TransformationKey;

    public string Name => "Sufiks";

    public ITransformation Create(ITransformation inner, TransformationParameters parameters)
    {
        return new SuffixTransformation(inner, parameters);
    }
}
