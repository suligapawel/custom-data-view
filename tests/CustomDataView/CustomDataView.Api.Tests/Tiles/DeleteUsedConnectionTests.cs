using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Tiles;

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
    }
}
