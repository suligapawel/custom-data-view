using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Exceptions;
using CustomDataView.Domain.Exceptions;
using CustomDataView.Domain.Repositories;

namespace CustomDataView.Application.Tiles.Commands;

public sealed record DeleteTile(Guid Id) : ICommand;

public sealed class DeleteTileHandler(ITileRepository repository)
{
    public void Handle(DeleteTile command)
    {
        _ = repository.Get(command.Id) ?? throw new NotFoundException(command.Id);
        var parents = repository.GetParents(command.Id);
        if (parents.Count > 0)
        {
            throw new TileInUseException(command.Id, parents);
        }

        repository.Delete(command.Id);
    }
}
