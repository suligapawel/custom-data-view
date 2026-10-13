namespace CustomDataView.Core.Transformations.Uppercase;

public sealed class UppercaseTransformationDefinition : ITransformationDefinition
{
    public const string TransformationKey = "uppercase";

    public string Key => TransformationKey;

    public string Name => "Wielkie litery";

    public string Description => "Zamienia tekst na wielkie litery w kulturze niezmiennej.";

    public IReadOnlyList<TransformationParameterDescriptor> Parameters => [];

    public ITransformation Create(ITransformation inner, TransformationParameters parameters)
    {
        return new UppercaseTransformation(inner, parameters);
    }
}
