using CustomDataView.Application.Abstractions;
using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Connections.Commands;

public sealed record AddConnection(ConnectionDefinition Definition) : ICommand<Guid>;

public sealed class AddConnectionHandler(IConnectionRepository repository)
{
    public Guid Handle(AddConnection command)
    {
        var connection = new Connection(Guid.NewGuid(), command.Definition);
        repository.Add(connection);
        return connection.Id;
    }
}
