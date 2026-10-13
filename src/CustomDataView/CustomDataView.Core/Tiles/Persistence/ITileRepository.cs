using CustomDataView.Core.Tiles.Definitions;

namespace CustomDataView.Core.Tiles.Persistence;

public interface ITileRepository
{
    void Add(Tile tile);
    Tile Get(Guid id);
    IReadOnlyList<Tile> GetAll();
}
