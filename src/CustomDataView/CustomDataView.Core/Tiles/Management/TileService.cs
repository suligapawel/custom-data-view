using CustomDataView.Core.Tiles.Definitions;
using CustomDataView.Core.Tiles.Persistence;

namespace CustomDataView.Core.Tiles.Management;

public sealed class TileService(ITileRepository repository) : ITileService
{
    public Guid Add(TileDefinition definition)
    {
        var tile = new Tile(Guid.NewGuid(), definition);
        repository.Add(tile);
        return tile.Id;
    }

    public IReadOnlyList<TileSummaryDto> GetAll()
        => repository.GetAll()
            .Select(tile => new TileSummaryDto(tile.Id, tile.Definition.Name))
            .ToList();

    public TileDto Get(Guid id)
    {
        var tile = repository.Get(id);
        return new TileDto(
            tile.Id,
            tile.Definition.Name,
            tile.Definition.Fields
                .Select(field => new TileFieldDto(
                    field.ConnectionId,
                    field.Path,
                    field.Label,
                    field.Transformations.Select(step => new TransformationStepDto(step.Key, step.Parameters)).ToList()))
                .ToList());
    }
}
