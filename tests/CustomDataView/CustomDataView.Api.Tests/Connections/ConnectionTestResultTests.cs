using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using CustomDataView.Core.Connections.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        Assert.That(result["elapsedMs"]?.GetValue<long>(), Is.GreaterThanOrEqualTo(0));
        Assert.That(JsonNode.DeepEquals(result["body"], JsonNode.Parse("""{"sellers": [{"name": "ACME"}]}""")), Is.True);
    }

    [Test(Description = "scenario_TEMP-007-014 - #37")]
    public async Task Given_ExternalApiRefusingConnections_When_UserTestsConnection_Then_TestFailsWithSourceUnavailable()
    {
        Factory.ExternalApi.Respond = (_, _) => throw new HttpRequestException("Connection refused");

        var response = await TestConnection();

        await AssertFailure(response, "source_unavailable");
    }

    [Test(Description = "scenario_TEMP-007-014 - #37")]
    public async Task Given_ExternalApiNotRespondingWithinTimeout_When_UserTestsConnection_Then_TestFailsWithSourceUnavailable()
    {
        Factory.Services.GetRequiredService<IOptions<ExternalApiOptions>>().Value.Timeout = TimeSpan.FromMilliseconds(200);
        Factory.ExternalApi.Respond = async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        };

        var response = await TestConnection();

        await AssertFailure(response, "source_unavailable");
    }

    private static async Task AssertFailure(HttpResponseMessage response, string code)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
        var result = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(result["code"]?.GetValue<string>(), Is.EqualTo(code));
    }

    private Task<HttpResponseMessage> TestConnection()
    {
        return Client.PostAsJsonAsync($"{ConnectionsUrl}/test", new
        {
            connection = new { name = "Sprzedawcy", method = "GET", url = "https://api.example.com/sellers" }
        });
    }
}
