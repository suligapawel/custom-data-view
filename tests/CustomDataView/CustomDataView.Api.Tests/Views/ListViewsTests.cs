using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Views;

public class ListViewsTests : ViewApiTests
{
    [Test(Description = "scenario_TEMP-024-028 - #17")]
    public async Task Given_ThreeViews_When_UserListsViews_Then_NamesAndTileCountsAreInAddingOrder()
    {
        var salesId = await CreateView("Sprzedaż", ViewTile(CustomerTileId, 0), ViewTile(RateTileId, 1));
        var emptyId = await CreateView("Pusty");
        var serviceId = await CreateView("Obsługa", ViewTile(CustomerTileId, 0), ViewTile(RateTileId, 1), ViewTile(OrdersTileId, 2));

        var views = await Client.GetFromJsonAsync<JsonArray>(ViewsUrl);

        Assert.That(
            views.Select(view => (view["id"]?.GetValue<Guid>(), view["name"]?.GetValue<string>(), view["tileCount"]?.GetValue<int>())),
            Is.EqualTo(new (Guid?, string, int?)[] { (salesId, "Sprzedaż", 2), (emptyId, "Pusty", 0), (serviceId, "Obsługa", 3) }));
    }

    [Test(Description = "scenario_TEMP-024-028 - #18")]
    public async Task Given_NoViews_When_UserListsViews_Then_ListIsEmpty()
    {
        var views = await Client.GetFromJsonAsync<JsonArray>(ViewsUrl);

        Assert.That(views, Is.Empty);
    }
}
