using CustomDataView.Core.Tiles.Definitions;

namespace CustomDataView.Core.Tiles.Persistence;

public interface ITileRepository
{
    void Add(Tile tile);
    IReadOnlyList<Tile> GetAll();
}
