namespace CustomDataView.Core.Transformations.Prefix;

public sealed class PrefixTransformationDefinition : ITransformationDefinition
{
    public const string TransformationKey = "prefix";

    public string Key => TransformationKey;

    public string Name => "Prefiks";

    public string Description => "Dokleja tekst przed wartością.";

    public IReadOnlyList<TransformationParameterDescriptor> Parameters { get; } =
        [new TransformationParameterDescriptor("text", TransformationParameterTypes.String)];

    public ITransformation Create(ITransformation inner, TransformationParameters parameters)
    {
        return new PrefixTransformation(inner, parameters);
    }
}
