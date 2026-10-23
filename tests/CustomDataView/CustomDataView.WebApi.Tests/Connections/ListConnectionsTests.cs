using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Connections;

public class ListConnectionsTests : ConnectionApiTests
{
    [Test(Description = "scenario_TEMP-007-014 - #4")]
    public async Task Given_TwoSavedConnections_When_UserBrowsesList_Then_ConnectionsAreInAddingOrder()
    {
        await CreateConnection(new
        {
            name = "Sprzedawcy kontraktu",
            method = "GET",
            url = "https://api.example.com/contracts/{id}/sellers",
            pathParameters = new[] { new { name = "id", required = true } }
        });
        await CreateConnection(new
        {
            name = "Wyszukiwanie sprzedawców",
            method = "POST",
            url = "https://api.example.com/sellers/search"
        });

        var connections = await Client.GetFromJsonAsync<JsonArray>(ConnectionsUrl);

        Assert.That(
            connections.Select(connection => connection["name"]!.GetValue<string>()),
            Is.EqualTo(new[] { "Sprzedawcy kontraktu", "Wyszukiwanie sprzedawców" }));
        Assert.That(
            connections.Select(connection => connection["method"]?.GetValue<string>()),
            Is.EqualTo(new[] { "GET", "POST" }));
        Assert.That(
            connections.Select(connection => connection["url"]?.GetValue<string>()),
            Is.EqualTo(new[] { "https://api.example.com/contracts/{id}/sellers", "https://api.example.com/sellers/search" }));
        Assert.That(connections.Select(connection => connection["id"]?.GetValue<Guid>()), Has.All.Not.EqualTo(Guid.Empty));
    }

    [Test(Description = "scenario_TEMP-007-014 - #20")]
    public async Task Given_NoConnections_When_UserBrowsesList_Then_ListIsEmpty()
    {
        var connections = await Client.GetFromJsonAsync<JsonArray>(ConnectionsUrl);

        Assert.That(connections, Is.Empty);
    }
}
