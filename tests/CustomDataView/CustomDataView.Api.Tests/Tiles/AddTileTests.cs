using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Tiles;

public class AddTileTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-015-023 - #1")]
    public async Task Given_NoTile_When_UserSavesTileWithFieldsFromTwoConnections_Then_TileIsSavedAndIdReturned()
    {
        var response = await Client.PostAsJsonAsync(TilesUrl, new
        {
            name = "Klient i kurs",
            fields = new[]
            {
                Field(CustomerConnectionId, "$.name", "Nazwa", Transformation("uppercase")),
                Field(RateConnectionId, "$.rate", "Kurs EUR", Transformation("round", new { decimals = 2 }))
            }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var created = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(Guid.TryParse(created?["id"]?.GetValue<string>(), out var id), Is.True);
        var tiles = await Client.GetFromJsonAsync<JsonArray>(TilesUrl);
        Assert.That(tiles.Select(tile => tile["id"]?.GetValue<Guid>()), Is.EqualTo(new Guid?[] { id }));
        Assert.That(tiles.Single()["name"]?.GetValue<string>(), Is.EqualTo("Klient i kurs"));
    }

    [Test(Description = "scenario_TEMP-015-023 - #2")]
    public async Task Given_ThreeFieldsFromCustomerConnection_When_UserSavesTile_Then_TileHasThreeFieldsUsingThatConnection()
    {
        var id = await CreateTile(new
        {
            name = "Klient",
            fields = new[]
            {
                Field(CustomerConnectionId, "$.name", "Nazwa"),
                Field(CustomerConnectionId, "$.balance", "Saldo"),
                Field(CustomerConnectionId, "$.address.city", "Miasto")
            }
        });

        var tile = await GetTile(id);

        Assert.That(
            tile["fields"]!.AsArray().Select(field => (
                field["connectionId"]?.GetValue<Guid>(),
                field["path"]?.GetValue<string>(),
                field["label"]?.GetValue<string>())),
            Is.EqualTo(new (Guid?, string, string)[]
            {
                (CustomerConnectionId, "$.name", "Nazwa"),
                (CustomerConnectionId, "$.balance", "Saldo"),
                (CustomerConnectionId, "$.address.city", "Miasto")
            }));
    }

    [Test(Description = "scenario_TEMP-015-023 - #6")]
    public async Task Given_TileNamedCustomer_When_UserSavesAnotherCustomerTileWithRepeatedLabels_Then_BothTilesAreListed()
    {
        var firstId = await CreateTile(new { name = "Klient", fields = new[] { Field(CustomerConnectionId, "$.name", "Nazwa") } });

        var response = await Client.PostAsJsonAsync(TilesUrl, new
        {
            name = "Klient",
            fields = new[]
            {
                Field(CustomerConnectionId, "$.name", "Wartość"),
                Field(CustomerConnectionId, "$.balance", "Wartość")
            }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var secondId = (await response.Content.ReadFromJsonAsync<CreatedTile>()).Id;
        var tiles = await Client.GetFromJsonAsync<JsonArray>(TilesUrl);
        Assert.That(
            tiles.Select(tile => (tile["id"]?.GetValue<Guid>(), tile["name"]?.GetValue<string>())),
            Is.EqualTo(new (Guid?, string)[] { (firstId, "Klient"), (secondId, "Klient") }));
        Assert.That(secondId, Is.Not.EqualTo(firstId));
    }
}
