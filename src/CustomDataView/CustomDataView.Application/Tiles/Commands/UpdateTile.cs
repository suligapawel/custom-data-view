using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Exceptions;
using CustomDataView.Application.Tiles.Validation;
using CustomDataView.Domain.Exceptions;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Commands;

public sealed record UpdateTile(Guid Id, TileDefinition Definition) : ICommand;

public sealed class UpdateTileHandler(ITileRepository repository, TileReferencesValidator referencesValidator)
{
    public void Handle(UpdateTile command)
    {
        var tile = repository.Get(command.Id) ?? throw new NotFoundException(command.Id);
        referencesValidator.EnsureValid(command.Definition, command.Id);
        var kept = Labels(command.Definition).ToHashSet();
        var uses = repository.GetFieldUses(command.Id)
            .Where(use => !kept.Contains(TileLabels.Normalize(use.Field)))
            .ToList();
        if (uses.Count > 0)
        {
            throw new FieldInUseException(
                command.Id,
                uses.Select(use => use.Field).Distinct().ToList(),
                [],
                uses.Select(use => use.Tile).Distinct().ToList());
        }

        repository.Update(tile with { Definition = command.Definition });
    }

    private static IEnumerable<string> Labels(TileDefinition definition)
        => definition.Fields.Select(field => field.Label)
            .Concat(definition.FormFields.Select(field => field.Label))
            .Select(TileLabels.Normalize);
}
