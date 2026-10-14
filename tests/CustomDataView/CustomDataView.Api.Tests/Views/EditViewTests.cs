using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Views;

public class EditViewTests : ViewApiTests
{
    [Test(Description = "scenario_TEMP-024-028 - #21")]
    public async Task Given_DashboardAndServiceViews_When_UserReplacesDashboard_Then_NameAndAllTilesAreReplacedAndListOrderIsKept()
    {
        var id = await CreateView("Pulpit", ViewTile(CustomerTileId, 0), ViewTile(RateTileId, 1));
        var serviceId = await CreateView("Obsługa");

        var response = await Client.PutAsJsonAsync($"{ViewsUrl}/{id}", new
        {
            name = "Pulpit klienta",
            tiles = new[] { ViewTile(OrdersTileId, 0, "col-12"), ViewTile(CustomerTileId, 4, "col-6") }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var view = await GetView(id);
        Assert.That(view["name"]?.GetValue<string>(), Is.EqualTo("Pulpit klienta"));
        Assert.That(Tiles(view), Is.EqualTo(new (Guid?, string, int?, string)[]
        {
            (OrdersTileId, "Zamówienia", 0, "col-12"),
            (CustomerTileId, "Klient", 4, "col-6")
        }));
        var views = await Client.GetFromJsonAsync<JsonArray>(ViewsUrl);
        Assert.That(views.Select(listed => listed["id"]?.GetValue<Guid>()), Is.EqualTo(new Guid?[] { id, serviceId }));
    }

    [Test(Description = "scenario_TEMP-024-028 - #22")]
    public async Task Given_DashboardWithCustomerTile_When_UserEditsItToRepeatCustomerTile_Then_EditIsRejectedAndViewIsUnchanged()
    {
        var id = await CreateView("Pulpit", ViewTile(CustomerTileId, 0));

        var response = await Client.PutAsJsonAsync($"{ViewsUrl}/{id}", new
        {
            name = "Pulpit klienta",
            tiles = new[] { ViewTile(CustomerTileId, 0), ViewTile(CustomerTileId, 1) }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Does.Contain("tiles[1].tileId"));
        var view = await GetView(id);
        Assert.That(view["name"]?.GetValue<string>(), Is.EqualTo("Pulpit"));
        Assert.That(Tiles(view), Is.EqualTo(new (Guid?, string, int?, string)[] { (CustomerTileId, "Klient", 0, null) }));
    }
}
