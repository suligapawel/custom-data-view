using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Tiles;

public class TilePreviewValuesTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-015-023 - #37")]
    public async Task Given_FieldsFromCustomerAndRate_When_UserPreviewsWithoutCustomer_Then_PreviewIsRejectedWithoutCalls()
    {
        var id = await CustomerAndRateTile();

        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", new
        {
            connections = new[] { new { connectionId = RateConnectionId, query = new { currency = "EUR" } } }
        });

        await AssertRejected(response, $"connections[{CustomerConnectionId}].parameters.path.customer_id");
        Assert.That(Factory.ExternalApi.Requests, Is.Empty);
    }

    [Test(Description = "scenario_TEMP-015-023 - #38")]
    public async Task Given_CustomerField_When_UserPreviewsWithCustomerValuesTwice_Then_PreviewIsRejectedWithoutCalls()
    {
        var id = await CreateTile(new { name = "Klient", fields = new[] { Field(CustomerConnectionId, "$.name", "Nazwa") } });

        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", new
        {
            connections = new[]
            {
                new { connectionId = CustomerConnectionId, path = new { customer_id = "42" } },
                new { connectionId = CustomerConnectionId, path = new { customer_id = "43" } }
            }
        });

        await AssertRejected(response, "connections[1].connectionId");
        Assert.That(Factory.ExternalApi.Requests, Is.Empty);
    }

    [Test(Description = "scenario_TEMP-015-023 - #39")]
    public async Task Given_OnlyCustomerField_When_UserPreviewsWithRateValuesToo_Then_RateValuesAreIgnored()
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
