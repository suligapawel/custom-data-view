using CustomDataView.Core.Exceptions;
using CustomDataView.Core.Tiles.Definitions;
using CustomDataView.Core.Tiles.Persistence;
using CustomDataView.Core.Tiles.Validation;

namespace CustomDataView.Core.Tiles.Management;

public sealed class TileService(ITileRepository repository, TileReferencesValidator referencesValidator) : ITileService
{
    public Guid Add(TileDefinition definition)
    {
        ValidateReferences(definition);
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

    public void Update(Guid id, TileDefinition definition)
    {
        var tile = repository.Get(id) ?? throw new NotFoundException(id);
        ValidateReferences(definition);
        repository.Update(tile with { Definition = definition });
    }

    public void Delete(Guid id)
    {
        _ = repository.Get(id) ?? throw new NotFoundException(id);
        repository.Delete(id);
    }

    private void ValidateReferences(TileDefinition definition)
    {
        var errors = referencesValidator.Validate(definition);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
