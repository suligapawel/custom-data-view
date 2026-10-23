using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class TileSourcesTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-033 - #1")]
    public async Task Given_TileWithCustomerAndRateSources_When_UserViewsAndPreviewsIt_Then_SourcesAreShownAndEachIsCalledOnce()
    {
        var id = await CreateTile(Tile("Klient", Field(CustomerConnectionId, "$.name", "Nazwa"), Field(RateConnectionId, "$.rate", "Kurs")));

        var tile = await GetTile(id);
        await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", CustomerAndRateValues());

        Assert.That(
            JsonNode.DeepEquals(
                tile["sources"],
                new JsonArray(
                    new JsonObject { ["key"] = "customer", ["kind"] = "connection", ["connectionId"] = CustomerConnectionId },
                    new JsonObject { ["key"] = "rate", ["kind"] = "connection", ["connectionId"] = RateConnectionId })),
            Is.True,
            tile["sources"]?.ToJsonString());
        Assert.That(tile["fields"]!.AsArray().Select(field => field["source"]?.GetValue<string>()), Is.EqualTo(new[] { "customer", "rate" }));
        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url).Order(), Is.EqualTo(new[] { CustomerUrl, RateUrl }));
    }

    [TestCase("duplicate", "sources[1].key")]
    [TestCase("slash", "sources[0].key")]
    [TestCase("parent", "sources[0].key")]
    [TestCase("no-connection", "sources[0].connectionId")]
    [TestCase("missing-connection", "sources[0].connectionId")]
    [TestCase("unknown-source", "fields[0].source")]
    [Test(Description = "scenario_TEMP-033 - #2")]
    public async Task Given_TileWithInvalidSource_When_UserSavesIt_Then_SaveIsRejectedWithSourceOrFieldError(string invalid, string errorKey)
    {
        var tile = Tile("Klient", Field(CustomerConnectionId, "$.name", "Nazwa"), Field(RateConnectionId, "$.rate", "Kurs"));
        var sources = tile["sources"]!.AsArray();
        switch (invalid)
        {
            case "duplicate":
                sources[1]!["key"] = "customer";
                tile["fields"]![1]!["source"] = "customer";
                break;
            case "slash":
                sources[0]!["key"] = "cust/omer";
                break;
            case "parent":
                sources[0]!["key"] = "parent";
                break;
            case "no-connection":
                sources[0]!.AsObject().Remove("connectionId");
                break;
            case "missing-connection":
                sources[0]!["connectionId"] = Guid.NewGuid();
                break;
            case "unknown-source":
                tile["fields"]![0]!["source"] = "orders";
                break;
        }

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Does.Contain(errorKey));
        Assert.That(await Client.GetFromJsonAsync<JsonArray>(TilesUrl), Is.Empty);
    }

    [Test(Description = "scenario_TEMP-033 - #3")]
    public async Task Given_TileWithCustomerSource_When_UserDeletesCustomerConnection_Then_DeletionIsRejectedWithTile()
    {
        var id = await CreateTile(Tile("Klient", Field(CustomerConnectionId, "$.name", "Nazwa")));

        var response = await Client.DeleteAsync($"{ConnectionsUrl}/{CustomerConnectionId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        var conflict = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(conflict["tiles"]!.AsArray().Select(tile => tile!["id"]?.GetValue<Guid>()), Is.EqualTo(new Guid?[] { id }));
    }
}
