using CustomDataView.Core.Exceptions;
using CustomDataView.Core.Tiles.Definitions;
using CustomDataView.Core.Tiles.Persistence;
using CustomDataView.Core.Tiles.Validation;

namespace CustomDataView.Core.Tiles.Management;

public sealed class TileService(ITileRepository repository, TileReferencesValidator referencesValidator) : ITileService
{
    public Guid Add(TileDefinition definition)
    {
        var errors = referencesValidator.Validate(definition);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var tile = new Tile(Guid.NewGuid(), definition);
        repository.Add(tile);
        return tile.Id;
    }

    public IReadOnlyList<TileSummaryDto> GetAll()
        => repository.GetAll()
            .Select(tile => new TileSummaryDto(tile.Id, tile.Definition.Name, tile.Definition.Fields.Count))
            .ToList();

    public TileDto Get(Guid id)
    {
        var tile = repository.Get(id) ?? throw new NotFoundException(id);
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
