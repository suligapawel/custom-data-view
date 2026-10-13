using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Tiles;

public class TileDraftPreviewTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-015-023 - #42")]
    public async Task Given_UnsavedCustomerAndRateTile_When_UserPreviewsIt_Then_ResultMatchesSavedTilePreview()
    {
        var tile = new
        {
            name = "Klient i kurs",
            fields = new[]
            {
                Field(
                    CustomerConnectionId,
                    "$.temp",
                    "Temperatura",
                    Transformation("round", new { decimals = 1 }),
                    Transformation("suffix", new { text = " °C" })),
                Field(CustomerConnectionId, "$.name", "Nazwa", Transformation("uppercase")),
                Field(RateConnectionId, "$.rate", "Kurs EUR", Transformation("round", new { decimals = 2 }))
            }
        };
        var id = await CreateTile(tile);
        var saved = await (await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", CustomerAndRateValues()))
            .Content.ReadFromJsonAsync<JsonObject>();
        var list = await Client.GetStringAsync(TilesUrl);

        var response = await Client.PostAsJsonAsync($"{TilesUrl}/preview", new
        {
            tile,
            connections = new object[]
            {
                new { connectionId = CustomerConnectionId, path = new { customer_id = "42" } },
                new { connectionId = RateConnectionId, query = new { currency = "EUR" } }
            }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var unsaved = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(
            JsonNode.DeepEquals(
                unsaved,
                new JsonObject { ["id"] = null, ["name"] = "Klient i kurs", ["fields"] = saved["fields"]!.DeepClone() }),
            Is.True,
            unsaved?.ToJsonString());
        Assert.That(await Client.GetStringAsync(TilesUrl), Is.EqualTo(list));
    }

    [Test(Description = "scenario_TEMP-015-023 - #43")]
    public async Task Given_UnsavedTileWithoutNameAndWithMissingConnection_When_UserPreviewsIt_Then_PreviewIsRejectedWithoutCalls()
    {
        var response = await Client.PostAsJsonAsync($"{TilesUrl}/preview", new
        {
            tile = new { fields = new[] { Field(Guid.NewGuid(), "$.name", "Nazwa") } }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(
            problem["errors"]!.AsObject().Select(error => error.Key),
            Is.SupersetOf(new[] { "tile.name", "tile.fields[0].connectionId" }));
        Assert.That(Factory.ExternalApi.Requests, Is.Empty);
    }
}
