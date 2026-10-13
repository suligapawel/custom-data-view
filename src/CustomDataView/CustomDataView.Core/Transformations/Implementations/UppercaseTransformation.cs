using CustomDataView.Core.Transformations.Abstractions;

namespace CustomDataView.Core.Transformations.Implementations;

public sealed class UppercaseTransformation : Transformation
{
    public override string Key => "uppercase";

    public override string Name => "Wielkie litery";

    public override string Description => "Zamienia tekst na wielkie litery w kulturze niezmiennej.";

    protected override object Transform(object value, TransformationParameters parameters)
        => TransformationValue.ToText(Key, value).ToUpperInvariant();
}
