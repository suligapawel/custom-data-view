namespace CustomDataView.Core.Transformations.DateFormat;

public sealed class DateFormatTransformationDefinition : ITransformationDefinition
{
    public const string TransformationKey = "date_format";

    public string Key => TransformationKey;

    public string Name => "Formatowanie daty";

    public ITransformation Create(ITransformation inner, TransformationParameters parameters)
    {
        return new DateFormatTransformation(inner, parameters);
    }
}
