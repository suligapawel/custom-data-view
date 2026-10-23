using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Exceptions;
using CustomDataView.Domain.Exceptions;
using CustomDataView.Domain.Repositories;

namespace CustomDataView.Application.Connections.Commands;

public sealed record DeleteConnection(Guid Id) : ICommand;

public sealed class DeleteConnectionHandler(IConnectionRepository repository, ITileRepository tiles)
{
    public void Handle(DeleteConnection command)
    {
        _ = repository.Get(command.Id) ?? throw new NotFoundException(command.Id);
        var usingTiles = tiles.GetUsingConnection(command.Id);
        if (usingTiles.Count > 0)
        {
            throw new ConnectionInUseException(command.Id, usingTiles);
        }

        repository.Delete(command.Id);
    }
}
