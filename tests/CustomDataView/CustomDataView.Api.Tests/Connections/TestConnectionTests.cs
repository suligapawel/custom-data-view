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
    }
}
