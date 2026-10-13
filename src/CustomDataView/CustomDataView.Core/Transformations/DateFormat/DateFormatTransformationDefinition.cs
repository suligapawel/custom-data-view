namespace CustomDataView.Core.Transformations.DateFormat;

public sealed class DateFormatTransformationDefinition : ITransformationDefinition
{
    public const string TransformationKey = "date_format";

    public string Key => TransformationKey;

    public string Name => "Formatowanie daty";

    public string Description => "Formatuje datę ISO 8601 według wzorca w kulturze niezmiennej.";

    public IReadOnlyList<TransformationParameterDescriptor> Parameters { get; } =
        [new TransformationParameterDescriptor("pattern", TransformationParameterTypes.String)];

    public ITransformation Create(ITransformation inner, TransformationParameters parameters)
    {
        return new DateFormatTransformation(inner, parameters);
    }
}
