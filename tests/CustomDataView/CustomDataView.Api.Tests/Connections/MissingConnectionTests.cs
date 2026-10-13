using System.Net;
using System.Net.Http.Json;

namespace CustomDataView.Api.Tests.Connections;

public class MissingConnectionTests : ConnectionApiTests
{
    [TestCase("GET", "")]
    [TestCase("PUT", "")]
    [TestCase("DELETE", "")]
    [Test(Description = "scenario_TEMP-007-014 - #23")]
    public async Task Given_UnknownConnectionId_When_UserActsOnConnection_Then_ConnectionIsNotFound(string method, string suffix)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), $"{ConnectionsUrl}/{Guid.NewGuid()}{suffix}");
        if (method == "PUT")
        {
            request.Content = JsonContent.Create(new { name = "Pogoda", method = "GET", url = "https://api.example.com/weather" });
        }

        var response = await Client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test(Description = "scenario_TEMP-007-014 - #23")]
    public async Task Given_ConnectionIdThatIsNotIdentifier_When_UserBrowsesDetails_Then_ConnectionIsNotFound()
    {
        var response = await Client.GetAsync($"{ConnectionsUrl}/abc");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
