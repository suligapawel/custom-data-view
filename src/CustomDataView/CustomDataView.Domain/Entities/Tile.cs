using CustomDataView.Domain.Exceptions;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Entities;

public sealed class Tile
{
    private Tile(Guid id, TileDefinition definition)
    {
        Id = id;
        Definition = definition;
    }

    public Guid Id { get; }

    public TileDefinition Definition { get; private set; }

    public static Tile Create(TileDefinition definition)
    {
        InvalidDefinitionException.ThrowIfAny(definition.Check());
        return new Tile(Guid.NewGuid(), definition);
    }

    public static Tile Restore(Guid id, TileDefinition definition) => new(id, definition);

    public void Redefine(TileDefinition definition, IReadOnlyList<TileFieldUse> fieldUses)
    {
        InvalidDefinitionException.ThrowIfAny(definition.Check());
        var kept = definition.OutputLabels.Select(TileLabels.Normalize).ToHashSet();
        var removed = fieldUses.Where(use => !kept.Contains(TileLabels.Normalize(use.Field))).ToList();
        if (removed.Count > 0)
        {
            throw new FieldInUseException(
                Id,
                removed.Select(use => use.Field).Distinct().ToList(),
                removed.Select(use => use.Tile).Distinct().ToList());
        }

        Definition = definition;
    }

    public void EnsureCanBeDeleted(IReadOnlyList<TileReference> parents, IReadOnlyList<TileReference> actionSources)
    {
        var references = parents.Concat(actionSources).DistinctBy(reference => reference.Id).ToList();
        if (references.Count > 0)
        {
            throw new TileInUseException(Id, references);
        }
    }
}
