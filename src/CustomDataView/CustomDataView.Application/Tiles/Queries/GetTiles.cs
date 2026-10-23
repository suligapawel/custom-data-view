using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Tiles.Dtos;
using CustomDataView.Domain.Repositories;

namespace CustomDataView.Application.Tiles.Queries;

public sealed record GetTiles : IQuery<IReadOnlyList<TileSummaryDto>>;

public sealed class GetTilesHandler(ITileRepository repository)
{
    public IReadOnlyList<TileSummaryDto> Handle(GetTiles query)
        => repository.GetAll()
            .Select(tile => new TileSummaryDto(tile.Id, tile.Definition.Name, tile.Definition.Fields.Count + tile.Definition.FormFields.Count))
            .ToList();
}
