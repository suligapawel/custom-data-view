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
        Assert.That(Guid.TryParse(created?["id"]?.GetValue<string>(), out _), Is.True);
    }
}
