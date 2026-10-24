using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Dictionaries;
using CustomDataView.Application.Exceptions;
using CustomDataView.Application.Tiles.Validation;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Commands;

public sealed record UpdateTile(Guid Id, TileDefinition Definition) : ICommand;

public sealed class UpdateTileHandler(
    ITileRepository repository,
    TileReferencesValidator referencesValidator,
    IDictionaryLoadQueue dictionaryLoads)
{
    public void Handle(UpdateTile command)
    {
        var tile = repository.Get(command.Id) ?? throw new NotFoundException(command.Id);
        referencesValidator.EnsureValid(command.Definition, command.Id);
        tile.Redefine(command.Definition, repository.GetFieldUses(command.Id));
        repository.Update(tile);
        dictionaryLoads.Enqueue(repository.GetDictionaryUses(command.Id).Select(use => new DictionaryReference(command.Id, use.ValueLabel)));
    }
}
