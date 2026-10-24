using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Exceptions;
using CustomDataView.Domain.Repositories;

namespace CustomDataView.Application.Connections.Commands;

public sealed record DeleteConnection(Guid Id) : ICommand;

public sealed class DeleteConnectionHandler(IConnectionRepository repository, ITileRepository tiles)
{
    public void Handle(DeleteConnection command)
    {
        var connection = repository.Get(command.Id) ?? throw new NotFoundException(command.Id);
        connection.EnsureCanBeDeleted(tiles.GetUsingConnection(command.Id));
        repository.Delete(command.Id);
    }
}
