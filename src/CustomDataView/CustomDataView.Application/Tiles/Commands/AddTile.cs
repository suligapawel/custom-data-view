using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Tiles.Validation;
using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Commands;

public sealed record AddTile(TileDefinition Definition) : ICommand<Guid>;

public sealed class AddTileHandler(ITileRepository repository, TileReferencesValidator referencesValidator)
{
    public Guid Handle(AddTile command)
    {
        referencesValidator.EnsureValid(command.Definition);
        var tile = Tile.Create(command.Definition);
        repository.Add(tile);
        return tile.Id;
    }
}
