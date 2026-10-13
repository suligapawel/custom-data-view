using System.Net.Http.Json;

namespace CustomDataView.Api.Tests.Connections;

public class TestConnectionTests : ConnectionApiTests
{
    [Test(Description = "scenario_TEMP-007-014 - #27")]
    public async Task Given_GetConnectionWithPathParameter_When_UserTestsItWithId42_Then_ExternalApiReceivesResolvedAddress()
    {
        var id = await CreateConnection(new
        {
            name = "Sprzedawcy kontraktu",
            method = "GET",
            url = "https://api.example.com/contracts/{id}/sellers",
            pathParameters = new[] { new { name = "id", required = true } }
        });

        await Client.PostAsJsonAsync($"{ConnectionsUrl}/{id}/test", new { parameters = new { path = new { id = "42" } } });

        Assert.That(
            Factory.ExternalApi.Requests.Select(request => request.Url),
            Is.EqualTo(new[] { "https://api.example.com/contracts/42/sellers" }));
        Assert.That(Factory.ExternalApi.Requests.Single().Method, Is.EqualTo("GET"));
    }

    [Test(Description = "scenario_TEMP-007-014 - #28")]
    public async Task Given_GetConnectionWithPathParameter_When_UserTestsItWithSlashInValue_Then_ValueIsEncoded()
    {
        var id = await CreateConnection(new
        {
            name = "Sprzedawcy kontraktu",
            method = "GET",
            url = "https://api.example.com/contracts/{id}/sellers",
            pathParameters = new[] { new { name = "id", required = true } }
        });

        await Client.PostAsJsonAsync($"{ConnectionsUrl}/{id}/test", new { parameters = new { path = new { id = "a/b" } } });

        Assert.That(Factory.ExternalApi.Requests.Single().Url, Is.EqualTo("https://api.example.com/contracts/a%2Fb/sellers"));
    }
}
