using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Transformations;

public sealed class PrefixTransformation : Transformation
{
    private static readonly TransformationParameterDescriptor TextParameter =
        new("text", TransformationParameterTypes.String);

    public override string Key => "prefix";

    public override string Name => "Prefiks";

    public override string Description => "Dokleja tekst przed wartością.";

    public override TransformationParameterDescriptor Parameter => TextParameter;

    protected override object Transform(object value, string parameter)
        => RequireText(parameter) + TransformationValue.ToText(Key, value);
}
