namespace CustomDataView.Core.Transformations.Suffix;

public sealed class SuffixTransformation : Transformation
{
    private static readonly TransformationParameterDescriptor TextParameter =
        new("text", TransformationParameterTypes.String);

    public override string Key => "suffix";

    public override string Name => "Sufiks";

    public override string Description => "Dokleja tekst po wartości.";

    public override IReadOnlyList<TransformationParameterDescriptor> Parameters { get; } = [TextParameter];

    protected override object Transform(object value, TransformationParameters parameters)
    {
        return TransformationValue.ToText(Key, value) + parameters.GetText(Key, TextParameter.Name);
    }
}
