namespace CustomDataView.Core.Transformations.Prefix;

public sealed class PrefixTransformationDefinition : ITransformationDefinition
{
    public const string TransformationKey = "prefix";

    public static readonly TransformationParameterDescriptor TextParameter =
        new("text", TransformationParameterTypes.String);

    public string Key => TransformationKey;

    public string Name => "Prefiks";

    public string Description => "Dokleja tekst przed wartością.";

    public IReadOnlyList<TransformationParameterDescriptor> Parameters { get; } = [TextParameter];

    public ITransformation Create(ITransformation inner, TransformationParameters parameters)
    {
        return new PrefixTransformation(inner, parameters);
    }
}
