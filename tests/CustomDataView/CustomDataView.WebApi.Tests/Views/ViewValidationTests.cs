using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Views;

public class ViewValidationTests : ViewApiTests
{
    [TestCase(null)]
    [TestCase("   ")]
    [TestCase(101)]
    [Test(Description = "scenario_TEMP-024-028 - #9")]
    public async Task Given_ViewWithInvalidName_When_UserSavesIt_Then_SaveIsRejectedWithNameError(object name)
    {
        var view = ValidView();
        view["name"] = name is int length ? new string('a', length) : (string)name;

        var response = await Client.PostAsJsonAsync(ViewsUrl, view);

        await AssertRejected(response, "name");
    }

    [Test(Description = "scenario_TEMP-024-028 - #10")]
    public async Task Given_EntryWithoutTile_When_UserSavesView_Then_SaveIsRejectedWithFirstTileError()
    {
        var response = await Client.PostAsJsonAsync(ViewsUrl, new { name = "Pulpit", tiles = new[] { new { position = 0 } } });

        await AssertRejected(response, "tiles[0].tileId");
    }

    [Test(Description = "scenario_TEMP-024-028 - #11")]
    public async Task Given_TileDoesNotExist_When_UserSavesViewWithIt_Then_SaveIsRejectedWithSecondTileErrorNamingIt()
    {
        var missingId = Guid.NewGuid();

        var response = await Client.PostAsJsonAsync(ViewsUrl, new
        {
            name = "Pulpit",
            tiles = new[] { ViewTile(CustomerTileId, 0), ViewTile(missingId, 1) }
        });

        var errors = await AssertRejected(response, "tiles[1].tileId");
        Assert.That(errors["tiles[1].tileId"]![0]!.GetValue<string>(), Does.Contain(missingId.ToString()));
        Assert.That(errors.Select(error => error.Key), Does.Not.Contain("tiles[0].tileId"));
    }

    [Test(Description = "scenario_TEMP-024-028 - #12")]
    public async Task Given_CustomerTileThreeTimes_When_UserSavesView_Then_SaveIsRejectedForEachRepetition()
    {
        var response = await Client.PostAsJsonAsync(ViewsUrl, new
        {
            name = "Pulpit",
            tiles = new[] { ViewTile(CustomerTileId, 0), ViewTile(CustomerTileId, 1), ViewTile(CustomerTileId, 2) }
        });

        var errors = await AssertRejected(response, "tiles[1].tileId");
        Assert.That(errors.Select(error => error.Key), Is.EquivalentTo(new[] { "tiles[1].tileId", "tiles[2].tileId" }));
    }

    [TestCase(null)]
    [TestCase(-1)]
    [Test(Description = "scenario_TEMP-024-028 - #13")]
    public async Task Given_TileWithMissingOrNegativePosition_When_UserSavesView_Then_SaveIsRejectedWithFirstPositionError(int? position)
    {
        var view = ValidView();
        view["tiles"]![0]!["position"] = position;

        var response = await Client.PostAsJsonAsync(ViewsUrl, view);

        await AssertRejected(response, "tiles[0].position");
    }

    [Test(Description = "scenario_TEMP-024-028 - #14")]
    public async Task Given_ThreeTilesAtSamePosition_When_UserSavesView_Then_SaveIsRejectedForEachRepeatedPosition()
    {
        var response = await Client.PostAsJsonAsync(ViewsUrl, new
        {
            name = "Pulpit",
            tiles = new[] { ViewTile(CustomerTileId, 2), ViewTile(RateTileId, 2), ViewTile(OrdersTileId, 2) }
        });

        var errors = await AssertRejected(response, "tiles[1].position");
        Assert.That(errors.Select(error => error.Key), Is.EquivalentTo(new[] { "tiles[1].position", "tiles[2].position" }));
        Assert.That(errors["tiles[2].position"]![0]!.GetValue<string>(), Does.Contain("2"));
    }

    [TestCase(200, HttpStatusCode.Created)]
    [TestCase(201, HttpStatusCode.BadRequest)]
    [Test(Description = "scenario_TEMP-024-028 - #15")]
    public async Task Given_CssClassOfGivenLength_When_UserSavesView_Then_OnlyClassUpTo200CharactersIsAccepted(
        int length, HttpStatusCode expected)
    {
        var view = ValidView();
        view["tiles"]![0]!["cssClass"] = $"  {new string('c', length)}  ";

        var response = await Client.PostAsJsonAsync(ViewsUrl, view);

        Assert.That(response.StatusCode, Is.EqualTo(expected));
        if (expected == HttpStatusCode.BadRequest)
        {
            await AssertRejected(response, "tiles[0].cssClass");
        }
    }

    [Test(Description = "scenario_TEMP-024-028 - #16")]
    public async Task Given_SalesView_When_UserSavesInvalidView_Then_NothingIsSaved()
    {
        var salesId = await CreateView("Sprzedaż");

        var response = await Client.PostAsJsonAsync(ViewsUrl, new { name = "Pulpit", tiles = new[] { ViewTile(CustomerTileId, -1) } });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var views = await Client.GetFromJsonAsync<JsonArray>(ViewsUrl);
        Assert.That(
            views.Select(view => (view["id"]?.GetValue<Guid>(), view["name"]?.GetValue<string>())),
            Is.EqualTo(new (Guid?, string)[] { (salesId, "Sprzedaż") }));
    }

    private JsonObject ValidView()
        => new()
        {
            ["name"] = "Pulpit",
            ["tiles"] = new JsonArray(
                new JsonObject { ["tileId"] = CustomerTileId, ["position"] = 0 },
                new JsonObject { ["tileId"] = RateTileId, ["position"] = 1 })
        };

    private static async Task<JsonObject> AssertRejected(HttpResponseMessage response, string errorKey)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        var errors = problem["errors"]!.AsObject();
        Assert.That(errors.Select(error => error.Key), Does.Contain(errorKey));
        return errors;
    }
}
