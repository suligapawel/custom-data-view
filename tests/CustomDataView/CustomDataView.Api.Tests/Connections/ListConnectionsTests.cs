using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Connections;

public class ListConnectionsTests
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

    [Test(Description = "scenario_TEMP-007-014 - #4")]
    public async Task Given_TwoSavedConnections_When_UserBrowsesList_Then_ConnectionsAreInAddingOrder()
    {
        await client.PostAsJsonAsync(ConnectionsUrl, new
        {
            name = "Sprzedawcy kontraktu",
            method = "GET",
            url = "https://api.example.com/contracts/{id}/sellers",
            pathParameters = new[] { new { name = "id", required = true } }
        });
        await client.PostAsJsonAsync(ConnectionsUrl, new
        {
            name = "Wyszukiwanie sprzedawców",
            method = "POST",
            url = "https://api.example.com/sellers/search"
        });

        var connections = await client.GetFromJsonAsync<JsonArray>(ConnectionsUrl);

        Assert.That(
            connections.Select(connection => connection["name"]!.GetValue<string>()),
            Is.EqualTo(new[] { "Sprzedawcy kontraktu", "Wyszukiwanie sprzedawców" }));
        Assert.That(
            connections.Select(connection => connection["method"]?.GetValue<string>()),
            Is.EqualTo(new[] { "GET", "POST" }));
    }
}
