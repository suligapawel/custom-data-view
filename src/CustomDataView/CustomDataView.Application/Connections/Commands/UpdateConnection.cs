using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Exceptions;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Connections.Commands;

public sealed record UpdateConnection(Guid Id, ConnectionDefinition Definition) : ICommand;

public sealed class UpdateConnectionHandler(IConnectionRepository repository)
{
    public void Handle(UpdateConnection command)
    {
        var connection = repository.Get(command.Id) ?? throw new NotFoundException(command.Id);
        repository.Update(connection with { Definition = command.Definition });
    }
}
