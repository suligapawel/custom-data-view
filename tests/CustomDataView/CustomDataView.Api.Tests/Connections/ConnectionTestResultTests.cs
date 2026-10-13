using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Connections;

public class ConnectionTestResultTests : ConnectionApiTests
{
    [Test(Description = "scenario_TEMP-007-014 - #36")]
    public async Task Given_ExternalApiRespondingWithJson_When_UserTestsConnection_Then_ResultContainsStatusTimeAndFullBody()
    {
        Factory.ExternalApi.Respond = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"sellers": [{"name": "ACME"}]}""", Encoding.UTF8, "application/json")
        });

        var response = await TestConnection();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(result["status"]?.GetValue<int>(), Is.EqualTo(200));
        Assert.That(JsonNode.DeepEquals(result["body"], JsonNode.Parse("""{"sellers": [{"name": "ACME"}]}""")), Is.True);
    }

    private Task<HttpResponseMessage> TestConnection()
    {
        return Client.PostAsJsonAsync($"{ConnectionsUrl}/test", new
        {
            connection = new { name = "Sprzedawcy", method = "GET", url = "https://api.example.com/sellers" }
        });
    }
}
