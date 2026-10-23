using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Transformations;

public sealed class UppercaseTransformation : Transformation
{
    public override string Key => "uppercase";

    public override string Name => "Wielkie litery";

    public override string Description => "Zamienia tekst na wielkie litery w kulturze niezmiennej.";

    protected override object Transform(object value, string parameter)
        => TransformationValue.ToText(Key, value).ToUpperInvariant();
}
