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

    [Test(Description = "scenario_TEMP-029 - #3")]
    public async Task Given_ViewWithCustomerAndRateTiles_When_UserPreviewsWithoutCustomerId_Then_OnlyCustomerTileHasParameterError()
    {
        var id = await CreateView("Pulpit klienta", ViewTile(CustomerTileId, 0), ViewTile(RateTileId, 1));

        var response = await Client.PostAsJsonAsync($"{ViewsUrl}/{id}/preview", new { parameters = new { currency = "EUR" } });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var preview = await response.Content.ReadFromJsonAsync<JsonObject>();
        var customer = preview["tiles"]![0]!.AsObject();
        Assert.That(customer["tileName"]?.GetValue<string>(), Is.EqualTo("Klient"));
        Assert.That(customer["error"]?["code"]?.GetValue<string>(), Is.EqualTo("invalid_parameters"));
        Assert.That(customer["error"]!["errors"]!.AsObject().Select(error => error.Key), Is.EqualTo(new[] { "parameters.customer_id" }));
        Assert.That(customer.ContainsKey("fields"), Is.False);
        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url), Is.EqualTo(new[] { RateUrl }));
        Assert.That(Fields(preview, 1), Is.EqualTo(new[] { ("Kurs", "4.32") }));
    }

    [Test(Description = "scenario_TEMP-029 - #4")]
    public async Task Given_RateSourceUnavailable_When_UserPreviewsView_Then_OnlyRateTileFieldHasSourceError()
    {
        var id = await CreateView("Pulpit klienta", ViewTile(CustomerTileId, 0), ViewTile(RateTileId, 1));
        Factory.ExternalApi.Respond = (request, _) => Task.FromResult(request.RequestUri!.AbsoluteUri == CustomerUrl
            ? Json(CustomerResponse)
            : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var response = await Client.PostAsJsonAsync($"{ViewsUrl}/{id}/preview", CustomerAndRateValues());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var preview = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(Fields(preview, 0), Is.EqualTo(new[] { ("Nazwa", "\"acme\"") }));
        Assert.That(preview["tiles"]![1]!["fields"]![0]!["error"]?["code"]?.GetValue<string>(), Is.EqualTo("source_unavailable"));
    }

    [Test(Description = "scenario_TEMP-029 - #5")]
    public async Task Given_TwoTilesUsingCustomerConnection_When_UserPreviewsView_Then_CustomerSourceIsCalledForEachTile()
    {
        var balanceTileId = await CreateTile(new { name = "Saldo", fields = new[] { Field(CustomerConnectionId, "$.balance", "Saldo") } });
        var id = await CreateView("Pulpit", ViewTile(CustomerTileId, 0), ViewTile(balanceTileId, 1));

        var response = await Client.PostAsJsonAsync($"{ViewsUrl}/{id}/preview", CustomerValues());

        var preview = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url), Is.EqualTo(new[] { CustomerUrl, CustomerUrl }));
        Assert.That(Fields(preview, 1), Is.EqualTo(new[] { ("Saldo", "1234.567") }));
    }

    private static IEnumerable<(string, string)> Fields(JsonObject preview, int tile)
        => preview["tiles"]![tile]!["fields"]!.AsArray()
            .Select(field => (field["label"]?.GetValue<string>(), field["value"]?.ToJsonString()));
}
