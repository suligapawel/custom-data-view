using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Views;

public class DeleteViewTests : ViewApiTests
{
    [Test(Description = "scenario_TEMP-024-028 - #24")]
    public async Task Given_Dashboard_When_UserDeletesIt_Then_ItDisappearsFromListAndDetailsAreNotFound()
    {
        var id = await CreateView("Pulpit", ViewTile(CustomerTileId, 0));

        var response = await Client.DeleteAsync($"{ViewsUrl}/{id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(await Client.GetFromJsonAsync<JsonArray>(ViewsUrl), Is.Empty);
        Assert.That((await Client.GetAsync($"{ViewsUrl}/{id}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test(Description = "scenario_TEMP-024-028 - #25")]
    public async Task Given_TwoViewsWithCustomerTile_When_UserDeletesOne_Then_TileConnectionAndOtherViewAreUnchanged()
    {
        var salesId = await CreateView("Sprzedaż", ViewTile(CustomerTileId, 0));
        var serviceId = await CreateView("Obsługa", ViewTile(CustomerTileId, 2, "col-6"));
        var tile = await Client.GetStringAsync($"{TilesUrl}/{CustomerTileId}");
        var connection = await Client.GetStringAsync($"{ConnectionsUrl}/{CustomerConnectionId}");

        var response = await Client.DeleteAsync($"{ViewsUrl}/{salesId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(await Client.GetStringAsync($"{TilesUrl}/{CustomerTileId}"), Is.EqualTo(tile));
        Assert.That(await Client.GetStringAsync($"{ConnectionsUrl}/{CustomerConnectionId}"), Is.EqualTo(connection));
        Assert.That(
            Tiles(await GetView(serviceId)),
            Is.EqualTo(new (Guid?, string, int?, string)[] { (CustomerTileId, "Klient", 2, "col-6") }));
    }
}
