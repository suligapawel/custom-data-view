using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Tiles;

public class EditTileTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-015-023 - #20")]
    public async Task Given_CustomerAndRateTiles_When_UserEditsCustomerTile_Then_NameAndFieldsAreReplacedInPlace()
    {
        var customerId = await CreateTile(new
        {
            name = "Klient",
            fields = new[] { Field(CustomerConnectionId, "$.name", "Nazwa"), Field(CustomerConnectionId, "$.balance", "Saldo") }
        });
        var rateId = await CreateTile(new { name = "Kurs", fields = new[] { Field(RateConnectionId, "$.rate", "Kurs EUR") } });

        var response = await Client.PutAsJsonAsync($"{TilesUrl}/{customerId}", new
        {
            name = "Klient VIP",
            fields = new[] { Field(CustomerConnectionId, "$.address.city", "Miasto"), Field(CustomerConnectionId, "$.name", "Nazwa") }
        });

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
}
