namespace CustomDataView.Core.Transformations.Suffix;

public sealed class SuffixTransformationDefinition : ITransformationDefinition
{
    public const string TransformationKey = "suffix";

    public string Key => TransformationKey;

    public string Name => "Sufiks";

    public string Description => "Dokleja tekst po wartości.";

    public IReadOnlyList<TransformationParameterDescriptor> Parameters { get; } =
        [new TransformationParameterDescriptor("text", TransformationParameterTypes.String)];

    public ITransformation Create(ITransformation inner, TransformationParameters parameters)
    {
        return new SuffixTransformation(inner, parameters);
    }
}
