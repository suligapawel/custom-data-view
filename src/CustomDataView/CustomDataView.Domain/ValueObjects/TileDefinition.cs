namespace CustomDataView.Domain.ValueObjects;

public sealed record TileDefinition(
    string Name,
    string Display,
    IReadOnlyList<TileSource> Sources,
    TileItems Items,
    IReadOnlyList<TileField> Fields)
{
    public IReadOnlyList<TileFormField> FormFields { get; init; } = [];

    public IReadOnlyList<TileChild> Children { get; init; } = [];

    public IReadOnlyList<TileBinding> Bindings { get; init; } = [];

    public TilePagination Pagination { get; init; }

    public IReadOnlyList<TileAction> Actions { get; init; } = [];

    public bool IsItemField(TileField field) => Items is not null && (field.Source == Items.Source || field.IsCombined);
}
