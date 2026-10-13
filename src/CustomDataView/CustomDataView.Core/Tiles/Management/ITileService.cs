using CustomDataView.Core.Tiles.Definitions;

namespace CustomDataView.Core.Tiles.Management;

public interface ITileService
{
    Guid Add(TileDefinition definition);
    IReadOnlyList<TileSummaryDto> GetAll();
    TileDto Get(Guid id);
    void Update(Guid id, TileDefinition definition);
}
