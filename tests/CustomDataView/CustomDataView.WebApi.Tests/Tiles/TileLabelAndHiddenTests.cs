using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using CustomDataView.WebApi.Tests.Views;

namespace CustomDataView.WebApi.Tests.Tiles;

public class TileLabelAndHiddenTests : ViewApiTests
{
    [TestCase("POST")]
    [TestCase("PUT")]
    [Test(Description = "scenario_TEMP-030 - #1")]
    public async Task Given_LabelRepeatedWithOtherCaseAndSpaces_When_UserSavesTile_Then_SaveIsRejectedWithSecondLabelError(string method)
    {
        var tile = Tile("Klient", Field(CustomerConnectionId, "$.name", "Nazwa"), Field(CustomerConnectionId, "$.balance", " nazwa "));
        var details = await Client.GetStringAsync($"{TilesUrl}/{CustomerTileId}");
        var tiles = await Client.GetStringAsync(TilesUrl);

        var response = method == "POST"
            ? await Client.PostAsJsonAsync(TilesUrl, tile)
            : await Client.PutAsJsonAsync($"{TilesUrl}/{CustomerTileId}", tile);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Is.EqualTo(new[] { "fields[1].label" }));
        Assert.That(await Client.GetStringAsync($"{TilesUrl}/{CustomerTileId}"), Is.EqualTo(details));
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

    [Test(Description = "scenario_TEMP-030 - #3")]
    public async Task Given_TileWithHiddenRoundedField_When_UserPreviewsTile_Then_HiddenFieldIsReturnedTransformedAndMarked()
    {
        var id = await CustomerWithHiddenBalance();

        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", CustomerValues());

        var preview = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(
            JsonNode.DeepEquals(
                preview["fields"],
                JsonNode.Parse("""[{"label": "Nazwa", "value": "acme"}, {"label": "Saldo", "value": 1234.6, "hidden": true}]""")),
            Is.True);
    }

    [Test(Description = "scenario_TEMP-030 - #4")]
    public async Task Given_ViewWithTileHavingHiddenField_When_UserPreviewsView_Then_HiddenFieldIsOmitted()
    {
        var tileId = await CustomerWithHiddenBalance();
        var viewId = await CreateView("Pulpit", ViewTile(tileId, 0));

        var response = await Client.PostAsJsonAsync($"{ViewsUrl}/{viewId}/preview", CustomerValues());

        var preview = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(
            preview["tiles"]![0]!["fields"]!.AsArray().Select(field => field["label"]?.GetValue<string>()),
            Is.EqualTo(new[] { "Nazwa" }));
    }

    private Task<Guid> CustomerWithHiddenBalance()
        => CreateTile(Tile("Klient", Field(CustomerConnectionId, "$.name", "Nazwa"),
                HiddenField(CustomerConnectionId, "$.balance", "Saldo", Transformation("round", new { decimals = 1 }))));
}
