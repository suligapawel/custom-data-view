using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Views;

public class AddViewTests : ViewApiTests
{
    [Test(Description = "scenario_TEMP-024-028 - #1")]
    public async Task Given_Tiles_When_UserSavesViewWithTwoTiles_Then_ViewIsSavedWithPositionsAndCssClasses()
    {
        var response = await Client.PostAsJsonAsync(ViewsUrl, new
        {
            name = "Pulpit klienta",
            tiles = new[] { ViewTile(CustomerTileId, 0, "col-6 highlight"), ViewTile(RateTileId, 1, "col-6") }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var created = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(Guid.TryParse(created?["id"]?.GetValue<string>(), out var id), Is.True);
        var view = await GetView(id);
        Assert.That(view["name"]?.GetValue<string>(), Is.EqualTo("Pulpit klienta"));
        Assert.That(Tiles(view), Is.EqualTo(new (Guid?, string, int?, string)[]
        {
            (CustomerTileId, "Klient", 0, "col-6 highlight"),
            (RateTileId, "Kurs walut", 1, "col-6")
        }));
    }

    [TestCase(true)]
    [TestCase(false)]
    [Test(Description = "scenario_TEMP-024-028 - #2")]
    public async Task Given_NoTiles_When_UserSavesEmptyView_Then_ViewIsSavedWithEmptyTileList(bool emptyList)
    {
        var view = new JsonObject { ["name"] = "Pusty" };
        if (emptyList)
        {
            view["tiles"] = new JsonArray();
        }

        var response = await Client.PostAsJsonAsync(ViewsUrl, view);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var created = await response.Content.ReadFromJsonAsync<CreatedView>();
        Assert.That(Tiles(await GetView(created.Id)), Is.Empty);
    }

    [Test(Description = "scenario_TEMP-024-028 - #3")]
    public async Task Given_TilesWithMissingBlankAndPaddedCssClasses_When_UserSavesView_Then_BlankClassesAreDroppedAndPaddingIsTrimmed()
    {
        var response = await Client.PostAsJsonAsync(ViewsUrl, new
        {
            name = "Pulpit",
            tiles = new[] { ViewTile(CustomerTileId, 0), ViewTile(RateTileId, 1, "   "), ViewTile(OrdersTileId, 2, "  col-12  ") }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var created = await response.Content.ReadFromJsonAsync<CreatedView>();
        Assert.That(Tiles(await GetView(created.Id)).Select(tile => tile.Item4), Is.EqualTo(new[] { null, null, "col-12" }));
    }
}
