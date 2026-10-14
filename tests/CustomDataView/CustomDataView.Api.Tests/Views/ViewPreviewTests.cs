using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Views;

public class ViewPreviewTests : ViewApiTests
{
    [Test(Description = "scenario_TEMP-029 - #1")]
    public async Task Given_ViewWithTwoTiles_When_UserPreviewsItWithParameters_Then_TilesAreInPositionOrderWithLayoutAndData()
    {
        var id = await CreateView("Pulpit klienta", ViewTile(RateTileId, 1), ViewTile(CustomerTileId, 0, "col-6"));

        var response = await Client.PostAsJsonAsync($"{ViewsUrl}/{id}/preview", CustomerAndRateValues());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var preview = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(preview["id"]?.GetValue<Guid>(), Is.EqualTo(id));
        Assert.That(preview["name"]?.GetValue<string>(), Is.EqualTo("Pulpit klienta"));
        Assert.That(Tiles(preview), Is.EqualTo(new (Guid?, string, int?, string)[]
        {
            (CustomerTileId, "Klient", 0, "col-6"),
            (RateTileId, "Kurs walut", 1, null)
        }));
        Assert.That(Fields(preview, 0), Is.EqualTo(new[] { ("Nazwa", "\"acme\"") }));
        Assert.That(Fields(preview, 1), Is.EqualTo(new[] { ("Kurs", "4.32") }));
    }

    [Test(Description = "scenario_TEMP-029 - #2")]
    public async Task Given_ViewWithTwoTiles_When_UserPreviewsItWithParametersInAddress_Then_ResultEqualsPostPreview()
    {
        var id = await CreateView("Pulpit klienta", ViewTile(CustomerTileId, 0), ViewTile(RateTileId, 1));
        var posted = await (await Client.PostAsJsonAsync($"{ViewsUrl}/{id}/preview", CustomerAndRateValues())).Content.ReadAsStringAsync();

        var response = await Client.GetAsync($"{ViewsUrl}/{id}/preview?customer_id=42&currency=EUR");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo(posted));
    }

    private static IEnumerable<(string, string)> Fields(JsonObject preview, int tile)
        => preview["tiles"]![tile]!["fields"]!.AsArray()
            .Select(field => (field["label"]?.GetValue<string>(), field["value"]?.ToJsonString()));
}
