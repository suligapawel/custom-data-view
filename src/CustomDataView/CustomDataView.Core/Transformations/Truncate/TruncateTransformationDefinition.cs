namespace CustomDataView.Core.Transformations.Truncate;

public sealed class TruncateTransformationDefinition : ITransformationDefinition
{
    public const string TransformationKey = "truncate";

    public string Key => TransformationKey;

    public string Name => "Przycięcie tekstu";

    public ITransformation Create(ITransformation inner, TransformationParameters parameters)
    {
        return new TruncateTransformation(inner, parameters);
    }
}
