using CustomDataView.Core.Transformations.Execution;

namespace CustomDataView.Core.Tiles.Definitions;

public sealed record TileInput(string Name, IReadOnlyList<TileFieldInput> Fields)
{
    public TileDefinition ToDefinition()
        => new TileDefinition(
            Name,
            Fields
                .Select(field => new TileField(
                    field.ConnectionId,
                    field.Path,
                    field.Label,
                    field.Transformations
                        .Select(step => new TransformationStep(step.Key, step.Parameters))
                        .ToList()))
                .ToList());
}
