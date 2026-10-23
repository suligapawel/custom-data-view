using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class DeleteUsedConnectionTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-015-023 - #25")]
    public async Task Given_TwoTilesUsingCustomerConnection_When_UserDeletesConnection_Then_DeletionIsRejectedAsConflict()
    {
        var customerId = await CreateTile(new { name = "Klient", fields = new[] { Field(CustomerConnectionId, "$.name", "Nazwa") } });
        var bothId = await CreateTile(new
        {
            name = "Klient i kurs",
            fields = new[] { Field(RateConnectionId, "$.rate", "Kurs EUR"), Field(CustomerConnectionId, "$.name", "Nazwa") }
        });

        var connection = await Client.GetStringAsync($"{ConnectionsUrl}/{CustomerConnectionId}");

        var response = await Client.DeleteAsync($"{ConnectionsUrl}/{CustomerConnectionId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        var conflict = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(
            JsonNode.DeepEquals(
                conflict,
                new JsonObject
                {
                    ["code"] = "connection_in_use",
                    ["tiles"] = new JsonArray(
                        new JsonObject { ["id"] = customerId, ["name"] = "Klient" },
                        new JsonObject { ["id"] = bothId, ["name"] = "Klient i kurs" })
                }),
            Is.True,
            conflict?.ToJsonString());
        Assert.That(await Client.GetStringAsync($"{ConnectionsUrl}/{CustomerConnectionId}"), Is.EqualTo(connection));
    }

    [Test(Description = "scenario_TEMP-015-023 - #26")]
    public async Task Given_OnlyTileUsingCustomerConnectionWasDeleted_When_UserDeletesConnection_Then_ConnectionIsDeleted()
    {
        var id = await CreateTile(new { name = "Klient", fields = new[] { Field(CustomerConnectionId, "$.name", "Nazwa") } });
        await Client.DeleteAsync($"{TilesUrl}/{id}");

        var response = await Client.DeleteAsync($"{ConnectionsUrl}/{CustomerConnectionId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await Client.GetAsync($"{ConnectionsUrl}/{CustomerConnectionId}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test(Description = "scenario_TEMP-015-023 - #27")]
    public async Task Given_NoTileUsesRateConnection_When_UserDeletesIt_Then_ConnectionIsDeleted()
    {
        await CreateTile(new { name = "Klient", fields = new[] { Field(CustomerConnectionId, "$.name", "Nazwa") } });

        var response = await Client.DeleteAsync($"{ConnectionsUrl}/{RateConnectionId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await Client.GetAsync($"{ConnectionsUrl}/{RateConnectionId}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
