using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class EditTileTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-015-023 - #20")]
    public async Task Given_CustomerAndRateTiles_When_UserEditsCustomerTile_Then_NameAndFieldsAreReplacedInPlace()
    {
        var customerId = await CreateTile(Tile("Klient", Field(CustomerConnectionId, "$.name", "Nazwa"), Field(CustomerConnectionId, "$.balance", "Saldo")));
        var rateId = await CreateTile(Tile("Kurs", Field(RateConnectionId, "$.rate", "Kurs EUR")));

        var response = await Client.PutAsJsonAsync($"{TilesUrl}/{customerId}", Tile("Klient VIP", Field(CustomerConnectionId, "$.address.city", "Miasto"), Field(CustomerConnectionId, "$.name", "Nazwa")));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var tile = await GetTile(customerId);
        Assert.That(tile["id"]?.GetValue<Guid>(), Is.EqualTo(customerId));
        Assert.That(tile["name"]?.GetValue<string>(), Is.EqualTo("Klient VIP"));
        Assert.That(
            tile["fields"]!.AsArray().Select(field => field["label"]?.GetValue<string>()),
            Is.EqualTo(new[] { "Miasto", "Nazwa" }));
        var tiles = await Client.GetFromJsonAsync<JsonArray>(TilesUrl);
        Assert.That(
            tiles.Select(saved => (saved["id"]?.GetValue<Guid>(), saved["name"]?.GetValue<string>())),
            Is.EqualTo(new (Guid?, string)[] { (customerId, "Klient VIP"), (rateId, "Kurs") }));
    }

    [Test(Description = "scenario_TEMP-015-023 - #21")]
    public async Task Given_CustomerTileWithNameField_When_UserRemovesAllFields_Then_EditIsRejectedAndTileIsUnchanged()
    {
        var id = await CreateTile(Tile("Klient", Field(CustomerConnectionId, "$.name", "Nazwa")));

        var response = await Client.PutAsJsonAsync($"{TilesUrl}/{id}", new { name = "Klient", fields = Array.Empty<object>() });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Does.Contain("fields"));
        var tile = await GetTile(id);
        Assert.That(
            tile["fields"]!.AsArray().Select(field => field["label"]?.GetValue<string>()),
            Is.EqualTo(new[] { "Nazwa" }));
    }
}
