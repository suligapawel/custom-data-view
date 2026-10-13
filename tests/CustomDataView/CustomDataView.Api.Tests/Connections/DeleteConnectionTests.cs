using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Connections;

public class DeleteConnectionTests : ConnectionApiTests
{
    [Test(Description = "scenario_TEMP-007-014 - #25")]
    public async Task Given_WeatherConnection_When_UserDeletesIt_Then_ItDisappearsFromListAndDetails()
    {
        var id = await CreateConnection(new { name = "Pogoda", method = "GET", url = "https://api.example.com/weather" });

        var response = await Client.DeleteAsync($"{ConnectionsUrl}/{id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var connections = await Client.GetFromJsonAsync<JsonArray>(ConnectionsUrl);
        Assert.That(connections, Is.Empty);
        var details = await Client.GetAsync($"{ConnectionsUrl}/{id}");
        Assert.That(details.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
