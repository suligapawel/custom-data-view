using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Exceptions;
using CustomDataView.Application.Tiles.Dtos;
using CustomDataView.Domain.Repositories;

namespace CustomDataView.Application.Tiles.Queries;

public sealed record GetTile(Guid Id) : IQuery<TileDto>;

public sealed class GetTileHandler(ITileRepository repository)
{
    public TileDto Handle(GetTile query)
    {
        var tile = repository.Get(query.Id) ?? throw new NotFoundException(query.Id);
        var definition = tile.Definition;
        return new TileDto(
            tile.Id,
            definition.Name,
            definition.Display,
            definition.Sources.Select(source => new TileSourceDto(source.Key, source.Kind, source.ConnectionId)).ToList(),
            definition.Items is null ? null : new TileItemsDto(definition.Items.Source, definition.Items.Path, definition.Items.Key),
            definition.Fields
                .Select(field => new TileFieldDto(
                    field.Source,
                    field.Path,
                    field.Label,
                    field.Transformations.Select(step => new TransformationStepDto(step.Key, step.Parameters)).ToList(),
                    field.Hidden,
                    field.Role))
                .ToList());
    }
}
