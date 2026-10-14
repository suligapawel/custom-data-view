using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Views;

public class TileChangesInViewsTests : ViewApiTests
{
    [Test(Description = "scenario_TEMP-024-028 - #7")]
    public async Task Given_ViewWithCustomerTile_When_UserEditsThatTile_Then_TileKeepsPositionAndCssClassUnderNewName()
    {
        var viewId = await CreateView("Pulpit", ViewTile(CustomerTileId, 3, "col-6"));

        var response = await Client.PutAsJsonAsync($"{TilesUrl}/{CustomerTileId}", new
        {
            name = "Klient VIP",
            fields = new[] { Field(CustomerConnectionId, "$.balance", "Saldo") }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(
            Tiles(await GetView(viewId)),
            Is.EqualTo(new (Guid?, string, int?, string)[] { (CustomerTileId, "Klient VIP", 3, "col-6") }));
    }

    [Test(Description = "scenario_TEMP-024-028 - #8")]
    public async Task Given_TwoViewsWithCustomerTile_When_UserDeletesThatTile_Then_ItDisappearsFromBothViews()
    {
        var salesId = await CreateView("Sprzedaż", ViewTile(CustomerTileId, 0), ViewTile(RateTileId, 1));
        var serviceId = await CreateView("Obsługa", ViewTile(CustomerTileId, 0), ViewTile(OrdersTileId, 1));

        var response = await Client.DeleteAsync($"{TilesUrl}/{CustomerTileId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(Tiles(await GetView(salesId)).Select(tile => tile.Item1), Is.EqualTo(new Guid?[] { RateTileId }));
        Assert.That(Tiles(await GetView(serviceId)).Select(tile => tile.Item1), Is.EqualTo(new Guid?[] { OrdersTileId }));
        var views = await Client.GetFromJsonAsync<JsonArray>(ViewsUrl);
        Assert.That(
            views.Select(view => (view["id"]?.GetValue<Guid>(), view["tileCount"]?.GetValue<int>())),
            Is.EqualTo(new (Guid?, int?)[] { (salesId, 1), (serviceId, 1) }));
    }
}
