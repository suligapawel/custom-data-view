using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using CustomDataView.WebApi.Tests.Views;

namespace CustomDataView.WebApi.Tests.Tiles;

public class ConnectionParameterValidationTests : ViewApiTests
{
    [Test(Description = "scenario_TEMP-031 - #1")]
    public async Task Given_CustomerAndRateTile_When_UserPreviewsWithCurrencyOnly_Then_OnlyCustomerFieldHasParameterError()
    {
        var id = await CustomerAndRateTile();

        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", new { parameters = new { currency = "EUR" } });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var preview = await response.Content.ReadFromJsonAsync<JsonObject>();
        AssertParameterError(preview["fields"]![0], "customer_id", bound: false);
        Assert.That(preview["fields"]![1]!["value"]?.GetValue<decimal>(), Is.EqualTo(4.31789m));
        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url), Is.EqualTo(new[] { RateUrl }));
    }

    [Test(Description = "scenario_TEMP-031 - #2")]
    public async Task Given_CustomerAndRateTile_When_UserPreviewsWithCustomerAsObject_Then_CustomerFieldHasParameterErrorWithoutCall()
    {
        var id = await CustomerAndRateTile();

        var response = await Client.PostAsJsonAsync(
            $"{TilesUrl}/{id}/preview",
            new { parameters = new { customer_id = new { id = 42 }, currency = "EUR" } });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var preview = await response.Content.ReadFromJsonAsync<JsonObject>();
        AssertParameterError(preview["fields"]![0], "customer_id", bound: false);
        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url), Is.EqualTo(new[] { RateUrl }));
    }

    [TestCase("DRAFT")]
    [TestCase("GET")]
    [Test(Description = "scenario_TEMP-031 - #3")]
    public async Task Given_CustomerAndRateTile_When_UserPreviewsDraftOrWithGetWithCurrencyOnly_Then_OnlyCustomerFieldHasParameterError(string method)
    {
        var id = await CustomerAndRateTile();

        var response = method == "GET"
            ? await Client.GetAsync($"{TilesUrl}/{id}/preview?currency=EUR")
            : await Client.PostAsJsonAsync($"{TilesUrl}/preview", new
            {
                tile = CustomerAndRate(),
                parameters = new { currency = "EUR" }
            });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var preview = await response.Content.ReadFromJsonAsync<JsonObject>();
        AssertParameterError(preview["fields"]![0], "customer_id", bound: false);
        Assert.That(preview["fields"]![1]!["value"]?.GetValue<decimal>(), Is.EqualTo(4.31789m));
    }

    [Test(Description = "scenario_TEMP-031 - #4")]
    public async Task Given_ViewWithCustomerAndRateTile_When_UserPreviewsView_Then_FieldHasParameterErrorInsteadOfTileError()
    {
        var viewId = await CreateView("Pulpit klienta", ViewTile(await CustomerAndRateTile(), 0));

        var response = await Client.PostAsJsonAsync($"{ViewsUrl}/{viewId}/preview", new { parameters = new { currency = "EUR" } });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var tile = (await response.Content.ReadFromJsonAsync<JsonObject>())["tiles"]![0]!.AsObject();
        Assert.That(tile.ContainsKey("error"), Is.False);
        Assert.That(tile["fields"]![0]!["error"]?["code"]?.GetValue<string>(), Is.EqualTo("invalid_parameters"));
    }

    private Task<Guid> CustomerAndRateTile() => CreateTile(CustomerAndRate());

    private object CustomerAndRate()
        => new
        {
            name = "Klient i kurs",
            fields = new[] { Field(CustomerConnectionId, "$.name", "Nazwa"), Field(RateConnectionId, "$.rate", "Kurs EUR") }
        };

    private static void AssertParameterError(JsonNode field, string parameter, bool bound)
    {
        Assert.That(field!["error"]?["code"]?.GetValue<string>(), Is.EqualTo("invalid_parameters"), field.ToJsonString());
        var parameters = field["error"]!["parameters"]!.AsArray();
        Assert.That(parameters.Select(error => error!["name"]?.GetValue<string>()), Is.EqualTo(new[] { parameter }));
        Assert.That(parameters[0]!["bound"]?.GetValue<bool>(), Is.EqualTo(bound));
        Assert.That(parameters[0]!["message"]?.GetValue<string>(), Is.Not.Empty);
        Assert.That(field.AsObject().ContainsKey("value"), Is.False);
    }
}
