using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Inputs;

public sealed record TileInput(string Name, IReadOnlyList<TileSourceInput> Sources, IReadOnlyList<TileFieldInput> Fields)
{
    public TileDefinition ToDefinition()
        => new TileDefinition(
            Name,
            Sources.Select(source => new TileSource(source.Key, source.Kind, source.ConnectionId)).ToList(),
            Fields
                .Select(field => new TileField(
                    field.Source,
                    field.Path,
                    field.Label,
                    field.Transformations
                        .Select(step => new TransformationStep(step.Key, step.Parameters))
                        .ToList(),
                    field.Hidden))
                .ToList());
}
