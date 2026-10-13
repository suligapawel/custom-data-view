using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Tiles;

public class TilePreviewTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-015-023 - #28")]
    public async Task Given_SavedTileWithFieldsFromTwoConnections_When_UserPreviewsIt_Then_PreviewHasTransformedValuesInOrder()
    {
        var id = await CreateTile(new
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
        });

        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", CustomerAndRateValues());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var preview = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(preview["id"]?.GetValue<Guid>(), Is.EqualTo(id));
        Assert.That(preview["name"]?.GetValue<string>(), Is.EqualTo("Klient i kurs"));
        Assert.That(
            preview["fields"]!.AsArray().Select(field => field["label"]?.GetValue<string>()),
            Is.EqualTo(new[] { "Temperatura", "Nazwa", "Kurs EUR" }));
    }
}
