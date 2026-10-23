using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Exceptions;
using CustomDataView.Domain.Repositories;

namespace CustomDataView.Application.Tiles.Commands;

public sealed record DeleteTile(Guid Id) : ICommand;

public sealed class DeleteTileHandler(ITileRepository repository)
{
    public void Handle(DeleteTile command)
    {
        _ = repository.Get(command.Id) ?? throw new NotFoundException(command.Id);
        repository.Delete(command.Id);
    }
}
