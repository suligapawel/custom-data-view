using System.Net.Http.Json;
using System.Text.Json.Nodes;

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

    [TestCase("{}", "https://api.example.com/sellers?units=metric&pageSize=20")]
    [TestCase("""{"pageSize": "50"}""", "https://api.example.com/sellers?units=metric&pageSize=50")]
    [TestCase("""{"status": "active"}""", "https://api.example.com/sellers?units=metric&pageSize=20&status=active")]
    [Test(Description = "scenario_TEMP-007-014 - #29")]
    public async Task Given_GetConnectionWithQueryParameters_When_UserTestsIt_Then_QueryIsAppended(string values, string address)
    {
        var id = await CreateConnection(new
        {
            name = "Sprzedawcy",
            method = "GET",
            url = "https://api.example.com/sellers?units=metric",
            queryParameters = new object[]
            {
                new { name = "pageSize", required = false, defaultValue = "20" },
                new { name = "status", required = false }
            }
        });

        await Client.PostAsJsonAsync(
            $"{ConnectionsUrl}/{id}/test",
            new JsonObject { ["parameters"] = new JsonObject { ["query"] = JsonNode.Parse(values) } });

        Assert.That(Factory.ExternalApi.Requests.Single().Url, Is.EqualTo(address));
    }

    [TestCase("{}", "https://api.example.com/sellers?tag=a&tag=b")]
    [TestCase("""{"tag": ["x", "y", "z"]}""", "https://api.example.com/sellers?tag=x&tag=y&tag=z")]
    [Test(Description = "scenario_TEMP-007-014 - #30")]
    public async Task Given_RepeatedQueryParameter_When_UserTestsIt_Then_EachValueIsSent(string values, string address)
    {
        var id = await CreateConnection(new
        {
            name = "Sprzedawcy",
            method = "GET",
            url = "https://api.example.com/sellers",
            queryParameters = new object[]
            {
                new { name = "tag", required = false, defaultValue = "a" },
                new { name = "tag", required = false, defaultValue = "b" }
            }
        });

        await Client.PostAsJsonAsync(
            $"{ConnectionsUrl}/{id}/test",
            new JsonObject { ["parameters"] = new JsonObject { ["query"] = JsonNode.Parse(values) } });

        Assert.That(Factory.ExternalApi.Requests.Single().Url, Is.EqualTo(address));
    }

    [Test(Description = "scenario_TEMP-007-014 - #31")]
    public async Task Given_PostConnectionWithBodyTemplate_When_UserTestsItWithValues_Then_ExternalApiReceivesResolvedBody()
    {
        var id = await CreateConnection(new
        {
            name = "Wyszukiwanie sprzedawców",
            method = "POST",
            url = "https://api.example.com/sellers/search",
            body = new { contractId = "{id}", limit = "{limit}" },
            bodyParameters = new object[]
            {
                new { name = "id", required = true },
                new { name = "limit", required = false, defaultValue = 10 }
            }
        });

        await Client.PostAsJsonAsync(
            $"{ConnectionsUrl}/{id}/test",
            new { parameters = new { body = new { id = "C-42", limit = 5 } } });

        var request = Factory.ExternalApi.Requests.Single();
        Assert.That(request.Method, Is.EqualTo("POST"));
        Assert.That(request.ContentType, Is.EqualTo("application/json"));
        Assert.That(JsonNode.DeepEquals(JsonNode.Parse(request.Body), JsonNode.Parse("""{"contractId": "C-42", "limit": 5}""")), Is.True);
    }
}
