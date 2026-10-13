using CustomDataView.Core.Transformations.Abstractions;

namespace CustomDataView.Core.Transformations.Implementations;

public sealed class PrefixTransformation : Transformation
{
    private static readonly TransformationParameterDescriptor TextParameter =
        new("text", TransformationParameterTypes.String);

    public override string Key => "prefix";

    public override string Name => "Prefiks";

    public override string Description => "Dokleja tekst przed wartością.";

    public override IReadOnlyList<TransformationParameterDescriptor> Parameters { get; } = [TextParameter];

    protected override object Transform(object value, TransformationParameters parameters)
    {
        return parameters.GetText(Key, TextParameter.Name) + TransformationValue.ToText(Key, value);
    }
}
