using CustomDataView.Domain.Entities;

namespace CustomDataView.Domain.Repositories;

public interface ITileRepository
{
    void Add(Tile tile);
    Tile Get(Guid id);
    IReadOnlyList<Tile> GetAll();
    void Update(Tile tile);
    void Delete(Guid id);
    IReadOnlyList<TileReference> GetUsingConnection(Guid connectionId);
    IReadOnlyList<TileReference> GetReferences(IReadOnlyCollection<Guid> ids);
    IReadOnlyList<TileReference> GetParents(Guid tileId);
    IReadOnlyList<TileReference> GetActionReferences(Guid tileId);
    IReadOnlyList<TileFieldUse> GetFieldUses(Guid tileId);
    IReadOnlyList<TileDictionaryUse> GetDictionaryUses(Guid tileId);
}
