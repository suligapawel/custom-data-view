using CustomDataView.Core.Transformations.Abstractions;

namespace CustomDataView.Core.Transformations.Implementations;

public sealed class LowercaseTransformation : Transformation
{
    public override string Key => "lowercase";

    public override string Name => "Małe litery";

    public override string Description => "Zamienia tekst na małe litery w kulturze niezmiennej.";

    protected override object Transform(object value, TransformationParameters parameters)
        => TransformationValue.ToText(Key, value).ToLowerInvariant();
}
