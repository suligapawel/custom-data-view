namespace CustomDataView.Domain.ValueObjects;

public sealed record TileDefinition(
    string Name,
    string Display,
    IReadOnlyList<TileSource> Sources,
    TileItems Items,
    IReadOnlyList<TileField> Fields)
{
    public IReadOnlyList<TileFormField> FormFields { get; init; } = [];

    public IReadOnlyList<TileBinding> Bindings { get; init; } = [];
}
