using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using CustomDataView.Infrastructure.ExternalApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CustomDataView.WebApi.Tests.Connections;

public class ConnectionTestResultTests : ConnectionApiTests
{
    [Test(Description = "scenario_TEMP-007-014 - #35")]
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

    [Test(Description = "scenario_TEMP-007-014 - #36")]
    public async Task Given_ExternalApiRefusingConnections_When_UserTestsConnection_Then_TestFailsWithSourceUnavailable()
    {
        Factory.ExternalApi.Respond = (_, _) => throw new HttpRequestException("Connection refused");

        var response = await TestConnection();

        await AssertFailure(response, "source_unavailable");
    }

    [Test(Description = "scenario_TEMP-007-014 - #36")]
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

    [TestCase(302)]
    [TestCase(404)]
    [TestCase(503)]
    [Test(Description = "scenario_TEMP-007-014 - #37")]
    public async Task Given_ExternalApiRespondingWithNonSuccessStatus_When_UserTestsConnection_Then_TestFailsWithUnexpectedStatus(int status)
    {
        Factory.ExternalApi.Respond = (_, _) =>
        {
            var response = new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent("{}") };
            response.Headers.Location = new Uri("https://api.example.com/elsewhere");
            return Task.FromResult(response);
        };

        var response = await TestConnection();

        var result = await AssertFailure(response, "unexpected_status");
        Assert.That(result["status"]?.GetValue<int>(), Is.EqualTo(status));
        Assert.That(Factory.ExternalApi.Requests, Has.Count.EqualTo(1));
    }

    [TestCase(200, "<html>…</html>")]
    [TestCase(204, "")]
    [Test(Description = "scenario_TEMP-007-014 - #38")]
    public async Task Given_ExternalApiRespondingWithInvalidJson_When_UserTestsConnection_Then_TestFailsWithInvalidJson(int status, string content)
    {
        Factory.ExternalApi.Respond = (_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
        {
            Content = new StringContent(content)
        });

        var response = await TestConnection();

        await AssertFailure(response, "invalid_json");
    }

    [Test(Description = "scenario_TEMP-007-014 - #39")]
    public async Task Given_ExternalApiRespondingWithMoreThanOneMegabyte_When_UserTestsConnection_Then_TestFailsWithResponseTooLarge()
    {
        var stream = new CountingStream(5 * 1024 * 1024);
        Factory.ExternalApi.Respond = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(stream)
        });

        var response = await TestConnection();

        await AssertFailure(response, "response_too_large");
        Assert.That(stream.BytesRead, Is.LessThan(2 * 1024 * 1024));
    }

    private static async Task<JsonObject> AssertFailure(HttpResponseMessage response, string code)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
        var result = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(result["code"]?.GetValue<string>(), Is.EqualTo(code));
        return result;
    }

    private Task<HttpResponseMessage> TestConnection()
        => Client.PostAsJsonAsync($"{ConnectionsUrl}/test", new
        {
            connection = new { name = "Sprzedawcy", method = "GET", url = "https://api.example.com/sellers" }
        });
}
