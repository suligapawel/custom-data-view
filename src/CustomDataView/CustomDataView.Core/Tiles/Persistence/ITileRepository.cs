using CustomDataView.Core.Tiles.Definitions;

namespace CustomDataView.Core.Tiles.Persistence;

public interface ITileRepository
{
    void Add(Tile tile);
    Tile Get(Guid id);
    IReadOnlyList<Tile> GetAll();
    void Update(Tile tile);
    void Delete(Guid id);
    IReadOnlyList<TileReference> GetUsingConnection(Guid connectionId);
    IReadOnlyList<TileReference> GetReferences(IReadOnlyCollection<Guid> ids);
}
