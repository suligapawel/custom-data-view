using CustomDataView.Domain.Exceptions;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Entities;

public sealed class Connection
{
    private Connection(Guid id, ConnectionDefinition definition)
    {
        Id = id;
        Definition = definition;
    }

    public Guid Id { get; }

    public ConnectionDefinition Definition { get; private set; }

    public static Connection Create(ConnectionDefinition definition)
    {
        InvalidDefinitionException.ThrowIfAny(definition.Check());
        return new Connection(Guid.NewGuid(), definition);
    }

    public static Connection Restore(Guid id, ConnectionDefinition definition) => new(id, definition);

    public void Redefine(ConnectionDefinition definition)
    {
        InvalidDefinitionException.ThrowIfAny(definition.Check());
        Definition = definition;
    }

    public void EnsureCanBeDeleted(IReadOnlyList<TileReference> usingTiles)
    {
        if (usingTiles.Count > 0)
        {
            throw new ConnectionInUseException(Id, usingTiles);
        }
    }
}
