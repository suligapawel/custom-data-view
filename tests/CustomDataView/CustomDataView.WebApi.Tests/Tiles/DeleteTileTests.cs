using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class DeleteTileTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-015-023 - #23")]
    public async Task Given_CustomerTileUsingCustomerConnection_When_UserDeletesTile_Then_TileDisappearsAndConnectionIsUnchanged()
    {
        var id = await CreateTile(Tile("Klient", Field(CustomerConnectionId, "$.name", "Nazwa"), Field(CustomerConnectionId, "$.balance", "Saldo")));
        var connection = await Client.GetStringAsync($"{ConnectionsUrl}/{CustomerConnectionId}");

        var response = await Client.DeleteAsync($"{TilesUrl}/{id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(await Client.GetFromJsonAsync<JsonArray>(TilesUrl), Is.Empty);
        Assert.That((await Client.GetAsync($"{TilesUrl}/{id}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await Client.GetStringAsync($"{ConnectionsUrl}/{CustomerConnectionId}"), Is.EqualTo(connection));
    }
}
