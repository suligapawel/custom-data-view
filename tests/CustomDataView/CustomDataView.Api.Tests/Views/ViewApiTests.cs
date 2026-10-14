using System.Net.Http.Json;
using CustomDataView.Api.Tests.Tiles;

namespace CustomDataView.Api.Tests.Views;

public abstract class ViewApiTests : TileApiTests
{
    protected const string ViewsUrl = "/api/custom-data-view/views";

    protected Guid CustomerTileId { get; private set; }
    protected Guid RateTileId { get; private set; }
    protected Guid OrdersTileId { get; private set; }

    [SetUp]
    public async Task CreateTiles()
    {
        CustomerTileId = await CreateTile(new { name = "Klient", fields = new[] { Field(CustomerConnectionId, "$.name", "Nazwa") } });
        RateTileId = await CreateTile(new { name = "Kurs walut", fields = new[] { Field(RateConnectionId, "$.rate", "Kurs") } });
        OrdersTileId = await CreateTile(new { name = "Zamówienia", fields = new[] { Field(CustomerConnectionId, "$.orders", "Zamówienia") } });
    }

    protected static object ViewTile(Guid tileId, int position, string cssClass = null) => new { tileId, position, cssClass };
}
