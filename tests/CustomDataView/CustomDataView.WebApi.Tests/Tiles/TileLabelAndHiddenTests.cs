using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class TileLabelAndHiddenTests : TileApiTests
{
    [TestCase("POST")]
    [TestCase("PUT")]
    [Test(Description = "scenario_TEMP-030 - #1")]
    public async Task Given_LabelRepeatedWithOtherCaseAndSpaces_When_UserSavesTile_Then_SaveIsRejectedWithSecondLabelError(string method)
    {
        var tile = Tile("Klient", Field(CustomerConnectionId, "$.name", "Nazwa"), Field(CustomerConnectionId, "$.balance", " nazwa "));
        var customerTileId = await CreateTile(Tile("Klient", Field(CustomerConnectionId, "$.name", "Nazwa")));
        var details = await Client.GetStringAsync($"{TilesUrl}/{customerTileId}");
        var tiles = await Client.GetStringAsync(TilesUrl);

        var response = method == "POST"
            ? await Client.PostAsJsonAsync(TilesUrl, tile)
            : await Client.PutAsJsonAsync($"{TilesUrl}/{customerTileId}", tile);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Is.EqualTo(new[] { "fields[1].label" }));
        Assert.That(await Client.GetStringAsync($"{TilesUrl}/{customerTileId}"), Is.EqualTo(details));
        Assert.That(await Client.GetStringAsync(TilesUrl), Is.EqualTo(tiles));
    }

    [Test(Description = "scenario_TEMP-030 - #2")]
    public async Task Given_TileWithVisibleAndHiddenField_When_UserViewsDetails_Then_HiddenFlagIsReturnedForEachField()
    {
        var id = await CustomerWithHiddenBalance();

        var tile = await GetTile(id);

        Assert.That(
            tile["fields"]!.AsArray().Select(field => (field["label"]?.GetValue<string>(), field["hidden"]?.GetValue<bool>())),
            Is.EqualTo(new (string, bool?)[] { ("Nazwa", false), ("Saldo", true) }));
    }

    private Task<Guid> CustomerWithHiddenBalance()
        => CreateTile(Tile("Klient", Field(CustomerConnectionId, "$.name", "Nazwa"),
                HiddenField(CustomerConnectionId, "$.balance", "Saldo", Transformation("round", 1))));
}
