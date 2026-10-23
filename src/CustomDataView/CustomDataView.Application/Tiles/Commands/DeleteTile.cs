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
        var references = repository.GetParents(command.Id)
            .Concat(repository.GetActionReferences(command.Id))
            .DistinctBy(reference => reference.Id)
            .ToList();
        if (references.Count > 0)
        {
            throw new TileInUseException(command.Id, references);
        }

        repository.Delete(command.Id);
    }
}
