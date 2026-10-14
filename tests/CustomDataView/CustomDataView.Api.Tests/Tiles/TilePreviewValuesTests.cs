using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Tiles;

public class TilePreviewValuesTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-015-023 - #37")]
    public async Task Given_FieldsFromCustomerAndRate_When_UserPreviewsWithCurrencyOnly_Then_PreviewIsRejectedWithoutCalls()
    {
        var id = await CustomerAndRateTile();

        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", new
        {
            parameters = new { currency = "EUR" }
        });

        await AssertRejected(response, "parameters.customer_id");
        Assert.That(Factory.ExternalApi.Requests, Is.Empty);
    }

    [Test(Description = "scenario_TEMP-015-023 - #38")]
    public async Task Given_RateAndArchiveRateFields_When_UserPreviewsWithCurrency_Then_BothSourcesGetCurrency()
    {
        var archiveConnectionId = await CreateConnection(new
        {
            name = "Kurs archiwalny",
            method = "GET",
            url = "https://api.example.com/archive-rates",
            queryParameters = new[] { new { name = "currency", required = false } }
        });
        var id = await CreateTile(new
        {
            name = "Kursy",
            fields = new[] { Field(RateConnectionId, "$.rate", "Kurs EUR"), Field(archiveConnectionId, "$.rate", "Kurs archiwalny") }
        });

        await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", new { parameters = new { currency = "EUR" } });

        Assert.That(
            Factory.ExternalApi.Requests.Select(request => request.Url).Order(),
            Is.EqualTo(new[] { "https://api.example.com/archive-rates?currency=EUR", RateUrl }));
    }

    [Test(Description = "scenario_TEMP-015-023 - #39")]
    public async Task Given_OnlyCustomerField_When_UserPreviewsWithCurrencyToo_Then_UndeclaredParameterIsIgnored()
    {
        var id = await CreateTile(new { name = "Klient", fields = new[] { Field(CustomerConnectionId, "$.name", "Nazwa") } });

        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", CustomerAndRateValues());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var preview = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(preview["fields"]![0]!["value"]?.GetValue<string>(), Is.EqualTo("acme"));
        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url), Is.EqualTo(new[] { CustomerUrl }));
    }

    [Test(Description = "scenario_TEMP-015-023 - #40")]
    public async Task Given_CustomerTile_When_UserPreviewsItTwice_Then_TileAndListAreUnchanged()
    {
        var id = await CreateTile(new { name = "Klient", fields = new[] { Field(CustomerConnectionId, "$.name", "Nazwa") } });
        var details = await Client.GetStringAsync($"{TilesUrl}/{id}");
        var list = await Client.GetStringAsync(TilesUrl);

        await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", CustomerValues());
        await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", CustomerValues());

        Assert.That(await Client.GetStringAsync($"{TilesUrl}/{id}"), Is.EqualTo(details));
        Assert.That(await Client.GetStringAsync(TilesUrl), Is.EqualTo(list));
    }

    [Test(Description = "scenario_TEMP-015-023 - #50")]
    public async Task Given_CustomerField_When_UserPreviewsWithCustomerAsNumber_Then_SourceGetsCustomerAsText()
    {
        var id = await CreateTile(new { name = "Klient", fields = new[] { Field(CustomerConnectionId, "$.name", "Nazwa") } });

        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", new { parameters = new { customer_id = 42 } });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url), Is.EqualTo(new[] { CustomerUrl }));
        var preview = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(preview["fields"]![0]!["value"]?.GetValue<string>(), Is.EqualTo("acme"));
    }

    [Test(Description = "scenario_TEMP-015-023 - #51")]
    public async Task Given_CustomerField_When_UserPreviewsWithCustomerAsObject_Then_PreviewIsRejectedWithoutCalls()
    {
        var id = await CreateTile(new { name = "Klient", fields = new[] { Field(CustomerConnectionId, "$.name", "Nazwa") } });

        var response = await Client.PostAsJsonAsync(
            $"{TilesUrl}/{id}/preview",
            new { parameters = new { customer_id = new { id = 42 } } });

        await AssertRejected(response, "parameters.customer_id");
        Assert.That(Factory.ExternalApi.Requests, Is.Empty);
    }

    [Test(Description = "scenario_TEMP-015-023 - #52")]
    public async Task Given_FieldsFromCustomerAndRate_When_UserPreviewsWithGet_Then_PreviewEqualsPostPreview()
    {
        var id = await CustomerAndRateTile();

        var response = await Client.GetAsync($"{TilesUrl}/{id}/preview?customer_id=42&currency=EUR");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(
            Factory.ExternalApi.Requests.Select(request => request.Url).Order(),
            Is.EqualTo(new[] { CustomerUrl, RateUrl }));
        var posted = await (await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", CustomerAndRateValues()))
            .Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(JsonNode.DeepEquals(await response.Content.ReadFromJsonAsync<JsonObject>(), posted), Is.True);
    }

    [Test(Description = "scenario_TEMP-015-023 - #53")]
    public async Task Given_RatesField_When_UserPreviewsWithGetAndRepeatedCurrency_Then_SourceGetsBothCurrencies()
    {
        var ratesConnectionId = await CreateConnection(new
        {
            name = "Kursy",
            method = "GET",
            url = "https://api.example.com/rates",
            queryParameters = new[] { new { name = "currency", required = false } }
        });
        var id = await CreateTile(new { name = "Kursy", fields = new[] { Field(ratesConnectionId, "$.rate", "Kursy") } });

        await Client.GetAsync($"{TilesUrl}/{id}/preview?currency=EUR&currency=USD");

        Assert.That(
            Factory.ExternalApi.Requests.Select(request => request.Url),
            Is.EqualTo(new[] { "https://api.example.com/rates?currency=EUR&currency=USD" }));
    }

    private Task<Guid> CustomerAndRateTile()
        => CreateTile(new
        {
            name = "Klient i kurs",
            fields = new[] { Field(CustomerConnectionId, "$.name", "Nazwa"), Field(RateConnectionId, "$.rate", "Kurs EUR") }
        });

    private static async Task AssertRejected(HttpResponseMessage response, string errorKey)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Does.Contain(errorKey));
    }
}
