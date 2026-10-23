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
