using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Tiles;

public class TileValidationTests : TileApiTests
{
    [TestCase(null)]
    [TestCase("   ")]
    [TestCase(101)]
    [Test(Description = "scenario_TEMP-015-023 - #7")]
    public async Task Given_TileWithInvalidName_When_UserSavesIt_Then_SaveIsRejectedWithNameError(object name)
    {
        var tile = ValidTile();
        tile["name"] = name is int length ? new string('a', length) : (string)name;

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        await AssertRejected(response, "name");
    }

    [TestCase(true)]
    [TestCase(false)]
    [Test(Description = "scenario_TEMP-015-023 - #8")]
    public async Task Given_TileWithoutFields_When_UserSavesIt_Then_SaveIsRejectedWithFieldsError(bool emptyList)
    {
        var tile = new JsonObject { ["name"] = "Pusty" };
        if (emptyList)
        {
            tile["fields"] = new JsonArray();
        }

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        await AssertRejected(response, "fields");
    }

    private JsonObject ValidTile()
        => new()
        {
            ["name"] = "Klient",
            ["fields"] = new JsonArray(
                new JsonObject { ["connectionId"] = CustomerConnectionId, ["path"] = "$.name", ["label"] = "Nazwa" },
                new JsonObject { ["connectionId"] = CustomerConnectionId, ["path"] = "$.balance", ["label"] = "Saldo" })
        };

    private static async Task AssertRejected(HttpResponseMessage response, string errorKey)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Does.Contain(errorKey));
    }
}
