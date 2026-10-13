namespace CustomDataView.Core.Transformations.DateFormat;

public sealed class DateFormatTransformationDefinition : ITransformationDefinition
{
    public const string TransformationKey = "date_format";

    public static readonly TransformationParameterDescriptor PatternParameter =
        new("pattern", TransformationParameterTypes.String);

    public string Key => TransformationKey;

    public string Name => "Formatowanie daty";

    public string Description => "Formatuje datę ISO 8601 według wzorca w kulturze niezmiennej.";

    public IReadOnlyList<TransformationParameterDescriptor> Parameters { get; } = [PatternParameter];

    public ITransformation Create(ITransformation inner, TransformationParameters parameters)
    {
        return new DateFormatTransformation(inner, parameters);
    }
}
