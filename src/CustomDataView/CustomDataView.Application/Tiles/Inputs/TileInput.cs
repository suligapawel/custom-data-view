using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Inputs;

public sealed record TileInput(string Name, IReadOnlyList<TileFieldInput> Fields)
{
    public TileDefinition ToDefinition()
        => new TileDefinition(
            Name,
            Fields
                .Select(field => new TileField(
                    field.ConnectionId.Value,
                    field.Path,
                    field.Label,
                    field.Transformations
                        .Select(step => new TransformationStep(step.Key, step.Parameters))
                        .ToList()))
                .ToList());
}
