using System.Net;
using System.Net.Http.Json;

namespace CustomDataView.Api.Tests.Connections;

public class AddConnectionTests
{
    private const string ConnectionsUrl = "/api/custom-data-view/connections";

    private CustomDataViewApplicationFactory factory;
    private HttpClient client;

    [SetUp]
    public void SetUp()
    {
        factory = new CustomDataViewApplicationFactory();
        client = factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        client.Dispose();
        factory.Dispose();
    }

    [Test(Description = "scenario_TEMP-007-014 - #1")]
    public async Task Given_NoContractSellersConnection_When_UserSavesGetConnectionWithPathParameter_Then_ConnectionIsSavedWithNewId()
    {
        var response = await client.PostAsJsonAsync(ConnectionsUrl, new
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
        var details = await client.GetAsync($"{ConnectionsUrl}/{created.Id}");
        Assert.That(details.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    private sealed record CreatedConnection(Guid Id);
}
