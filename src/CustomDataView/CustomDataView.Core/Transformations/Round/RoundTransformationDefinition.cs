namespace CustomDataView.Core.Transformations.Round;

public sealed class RoundTransformationDefinition : ITransformationDefinition
{
    public const string TransformationKey = "round";

    public string Key => TransformationKey;

    public string Name => "Zaokrąglenie";

    public string Description => "Zaokrągla liczbę do podanej liczby miejsc po przecinku (0-10).";

    public ITransformation Create(ITransformation inner, TransformationParameters parameters)
    {
        return new RoundTransformation(inner, parameters);
    }
}
