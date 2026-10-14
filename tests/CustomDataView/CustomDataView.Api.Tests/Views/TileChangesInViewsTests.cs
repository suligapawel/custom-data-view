using System.Net;
using System.Net.Http.Json;

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
}
