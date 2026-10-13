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
