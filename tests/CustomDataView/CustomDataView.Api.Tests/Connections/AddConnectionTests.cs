using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Connections;

public class AddConnectionTests : ConnectionApiTests
{
    [Test(Description = "scenario_TEMP-007-014 - #1")]
    public async Task Given_NoContractSellersConnection_When_UserSavesGetConnectionWithPathParameter_Then_ConnectionIsSavedWithNewId()
    {
        var response = await Client.PostAsJsonAsync(ConnectionsUrl, new
        {
            name = "Sprzedawcy kontraktu",
            method = "GET",
            url = "https://api.example.com/contracts/{id}/sellers?units=metric",
            headers = new[] { new { name = "Accept-Language", value = "pl" } },
            pathParameters = new[] { new { name = "id", required = true } },
            queryParameters = new[] { new { name = "pageSize", required = false, defaultValue = "20" } }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var created = await response.Content.ReadFromJsonAsync<CreatedConnection>();
        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        var details = await Client.GetAsync($"{ConnectionsUrl}/{created.Id}");
        Assert.That(details.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test(Description = "scenario_TEMP-007-014 - #2")]
    public async Task Given_PostConnectionDefinition_When_UserSavesItWithBodyTemplate_Then_DetailsShowUnchangedBodyTemplate()
    {
        var response = await Client.PostAsJsonAsync(ConnectionsUrl, new
        {
            name = "Wyszukiwanie sprzedawców",
            method = "POST",
            url = "https://api.example.com/sellers/search",
            body = new { contractId = "{id}", limit = "{limit}" },
            bodyParameters = new object[]
            {
                new { name = "id", required = true },
                new { name = "limit", required = false, defaultValue = 10 }
            }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var created = await response.Content.ReadFromJsonAsync<CreatedConnection>();
        var details = await Client.GetFromJsonAsync<JsonObject>($"{ConnectionsUrl}/{created.Id}");
        Assert.That(
            JsonNode.DeepEquals(details["body"], JsonNode.Parse("""{"contractId": "{id}", "limit": "{limit}"}""")),
            Is.True);
    }

    [Test(Description = "scenario_TEMP-007-014 - #3")]
    public async Task Given_PostConnectionWithoutBodyTemplate_When_UserSavesIt_Then_ConnectionIsSaved()
    {
        var response = await Client.PostAsJsonAsync(ConnectionsUrl, new
        {
            name = "Odświeżenie danych",
            method = "POST",
            url = "https://api.example.com/refresh"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var created = await response.Content.ReadFromJsonAsync<CreatedConnection>();
        var details = await Client.GetFromJsonAsync<JsonObject>($"{ConnectionsUrl}/{created.Id}");
        Assert.That(details["body"], Is.Null);
    }
}
