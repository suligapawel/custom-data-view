using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class ListTilesTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-015-023 - #16")]
    public async Task Given_CustomerTileThenRateTile_When_UserViewsTileList_Then_TilesAreInAddingOrderWithFieldCounts()
    {
        var customerId = await CreateTile(Tile("Klient", Field(CustomerConnectionId, "$.name", "Nazwa"),
                Field(CustomerConnectionId, "$.balance", "Saldo"),
                Field(CustomerConnectionId, "$.address.city", "Miasto")));
        var rateId = await CreateTile(Tile("Kurs", Field(RateConnectionId, "$.rate", "Kurs EUR")));

        var tiles = await Client.GetFromJsonAsync<JsonArray>(TilesUrl);

        Assert.That(
            tiles.Select(tile => (
                tile["id"]?.GetValue<Guid>(),
                tile["name"]?.GetValue<string>(),
                tile["fieldCount"]?.GetValue<int>())),
            Is.EqualTo(new (Guid?, string, int?)[] { (customerId, "Klient", 3), (rateId, "Kurs", 1) }));
    }

    [Test(Description = "scenario_TEMP-015-023 - #17")]
    public async Task Given_NoTiles_When_UserViewsTileList_Then_ListIsEmpty()
    {
        var tiles = await Client.GetFromJsonAsync<JsonArray>(TilesUrl);

        Assert.That(tiles, Is.Empty);
    }
}
