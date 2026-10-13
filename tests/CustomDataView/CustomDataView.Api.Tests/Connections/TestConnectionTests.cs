using System.Net;
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

    [TestCase("10", """{"contractId": "C-42", "limit": 10}""")]
    [TestCase(null, """{"contractId": "C-42", "limit": null}""")]
    [Test(Description = "scenario_TEMP-007-014 - #32")]
    public async Task Given_OptionalBodyParameter_When_UserTestsItWithoutValue_Then_DefaultOrNullIsSent(string defaultValue, string body)
    {
        var limit = new JsonObject { ["name"] = "limit", ["required"] = false };
        if (defaultValue is not null)
        {
            limit["defaultValue"] = JsonNode.Parse(defaultValue);
        }

        var id = await CreateConnection(new JsonObject
        {
            ["name"] = "Wyszukiwanie sprzedawców",
            ["method"] = "POST",
            ["url"] = "https://api.example.com/sellers/search",
            ["body"] = new JsonObject { ["contractId"] = "{id}", ["limit"] = "{limit}" },
            ["bodyParameters"] = new JsonArray(new JsonObject { ["name"] = "id", ["required"] = true }, limit)
        });

        await Client.PostAsJsonAsync($"{ConnectionsUrl}/{id}/test", new { parameters = new { body = new { id = "C-42" } } });

        Assert.That(JsonNode.DeepEquals(JsonNode.Parse(Factory.ExternalApi.Requests.Single().Body), JsonNode.Parse(body)), Is.True);
    }

    [TestCase("{}", "parameters.path.id")]
    [TestCase("""{"id": "42", "foo": "x"}""", "parameters.path.foo")]
    [TestCase("""{"id": ""}""", "parameters.path.id")]
    [TestCase("""{"id": 42}""", "parameters.path.id")]
    [Test(Description = "scenario_TEMP-007-014 - #33")]
    public async Task Given_GetConnectionWithRequiredPathParameter_When_UserTestsItWithInvalidValues_Then_TestIsRejected(
        string pathValues,
        string errorKey)
    {
        var id = await CreateConnection(new
        {
            name = "Sprzedawcy kontraktu",
            method = "GET",
            url = "https://api.example.com/contracts/{id}/sellers",
            pathParameters = new[] { new { name = "id", required = true } }
        });

        var response = await Client.PostAsJsonAsync(
            $"{ConnectionsUrl}/{id}/test",
            new JsonObject { ["parameters"] = new JsonObject { ["path"] = JsonNode.Parse(pathValues) } });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().ContainsKey(errorKey), Is.True);
        Assert.That(Factory.ExternalApi.Requests, Is.Empty);
    }

    [Test(Description = "scenario_TEMP-007-014 - #34")]
    public async Task Given_SavedAndUnsavedIdenticalConnections_When_UserTestsBoth_Then_TheySendSameRequestAndGetSameResult()
    {
        var definition = new JsonObject
        {
            ["name"] = "Sprzedawcy kontraktu",
            ["method"] = "GET",
            ["url"] = "https://api.example.com/contracts/{id}/sellers",
            ["pathParameters"] = new JsonArray(new JsonObject { ["name"] = "id", ["required"] = true })
        };
        var id = await CreateConnection(definition.DeepClone());
        var parameters = new JsonObject { ["path"] = new JsonObject { ["id"] = "42" } };

        var saved = await Client.PostAsJsonAsync($"{ConnectionsUrl}/{id}/test", new JsonObject { ["parameters"] = parameters.DeepClone() });
        var unsaved = await Client.PostAsJsonAsync(
            $"{ConnectionsUrl}/test",
            new JsonObject { ["connection"] = definition.DeepClone(), ["parameters"] = parameters.DeepClone() });

        Assert.That(unsaved.StatusCode, Is.EqualTo(saved.StatusCode));
        Assert.That(Factory.ExternalApi.Requests, Has.Count.EqualTo(2));
        Assert.That(Factory.ExternalApi.Requests[1], Is.EqualTo(Factory.ExternalApi.Requests[0]));
        Assert.That(await unsaved.Content.ReadAsStringAsync(), Is.EqualTo(await saved.Content.ReadAsStringAsync()));
    }

    [Test(Description = "scenario_TEMP-007-014 - #35")]
    public async Task Given_UnsavedDefinitionWithFtpUrl_When_UserTestsIt_Then_TestIsRejectedWithUrlError()
    {
        var response = await Client.PostAsJsonAsync($"{ConnectionsUrl}/test", new
        {
            connection = new { name = "Pogoda", method = "GET", url = "ftp://api.example.com" }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().ContainsKey("connection.url"), Is.True);
        Assert.That(Factory.ExternalApi.Requests, Is.Empty);
    }

    [Test(Description = "scenario_TEMP-007-014 - #41")]
    public async Task Given_OneSavedConnection_When_UserTestsSavedAndUnsavedConnections_Then_ConfigurationIsUnchanged()
    {
        var id = await CreateConnection(new
        {
            name = "Sprzedawcy kontraktu",
            method = "GET",
            url = "https://api.example.com/contracts/{id}/sellers",
            pathParameters = new[] { new { name = "id", required = true } }
        });
        var before = await Client.GetStringAsync($"{ConnectionsUrl}/{id}");

        await Client.PostAsJsonAsync($"{ConnectionsUrl}/{id}/test", new { parameters = new { path = new { id = "42" } } });
        await Client.PostAsJsonAsync($"{ConnectionsUrl}/test", new
        {
            connection = new { name = "Pogoda", method = "GET", url = "https://api.example.com/weather" }
        });

        var connections = await Client.GetFromJsonAsync<JsonArray>(ConnectionsUrl);
        Assert.That(connections, Has.Count.EqualTo(1));
        Assert.That(await Client.GetStringAsync($"{ConnectionsUrl}/{id}"), Is.EqualTo(before));
    }

    [Test(Description = "scenario_TEMP-007-014 - #42")]
    public async Task Given_PostConnectionWithHeaders_When_UserTestsIt_Then_ExternalApiReceivesHeaders()
    {
        var id = await CreateConnection(new
        {
            name = "Wyszukiwanie sprzedawców",
            method = "POST",
            url = "https://api.example.com/sellers/search",
            headers = new[]
            {
                new { name = "Accept-Language", value = "pl" },
                new { name = "Content-Type", value = "application/vnd.api+json" }
            },
            body = new { status = "active" }
        });

        await Client.PostAsJsonAsync($"{ConnectionsUrl}/{id}/test", new { });

        var request = Factory.ExternalApi.Requests.Single();
        Assert.That(request.Headers, Does.Contain("Accept-Language: pl"));
        Assert.That(request.ContentType, Is.EqualTo("application/vnd.api+json"));
    }

    [Test(Description = "scenario_TEMP-007-014 - #43")]
    public async Task Given_OptionalPathParameterWithDefault_When_UserTestsWithoutValue_Then_DefaultIsUsedInAddress()
    {
        var id = await CreateConnection(new
        {
            name = "Sprzedawcy",
            method = "GET",
            url = "https://api.example.com/{version}/sellers",
            pathParameters = new[] { new { name = "version", required = false, defaultValue = "v1" } }
        });

        await Client.PostAsJsonAsync($"{ConnectionsUrl}/{id}/test", new { });

        Assert.That(Factory.ExternalApi.Requests.Single().Url, Is.EqualTo("https://api.example.com/v1/sellers"));
    }
}
