using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Inputs;

public sealed record TileInput(
    string Name,
    string Display,
    IReadOnlyList<TileSourceInput> Sources,
    TileItemsInput Items,
    IReadOnlyList<TileFieldInput> Fields)
{
    public TileDefinition ToDefinition()
        => new TileDefinition(
            Name,
            Display,
            Sources.Select(source => new TileSource(source.Key, source.Kind, source.ConnectionId)).ToList(),
            Items is null ? null : new TileItems(Items.Source, Items.Path, Items.Key),
            Fields
                .Select(field => new TileField(
                    field.Source,
                    field.Path,
                    field.Label,
                    field.Transformations
                        .Select(step => new TransformationStep(step.Key, step.Parameters))
                        .ToList(),
                    field.Hidden,
                    string.IsNullOrWhiteSpace(field.Role) ? null : field.Role))
                .ToList());
}
