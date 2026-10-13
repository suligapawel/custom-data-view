namespace CustomDataView.Core.Transformations.Truncate;

public sealed class TruncateTransformationDefinition : ITransformationDefinition
{
    public const string TransformationKey = "truncate";

    public static readonly TransformationParameterDescriptor MaxLengthParameter =
        new("max_length", TransformationParameterTypes.Integer);

    public string Key => TransformationKey;

    public string Name => "Przycięcie tekstu";

    public string Description => "Przycina tekst do maksymalnej liczby znaków.";

    public IReadOnlyList<TransformationParameterDescriptor> Parameters { get; } = [MaxLengthParameter];

    public ITransformation Create(ITransformation inner, TransformationParameters parameters)
    {
        return new TruncateTransformation(inner, parameters);
    }
}
