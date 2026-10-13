using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Connections;

public class ConnectionValidationTests : ConnectionApiTests
{
    private static readonly string[] InvalidNames = [null, "   ", new string('a', 101)];

    [TestCaseSource(nameof(InvalidNames))]
    [Test(Description = "scenario_TEMP-007-014 - #7")]
    public async Task Given_InvalidName_When_UserSavesConnection_Then_SavingIsRejectedWithNameError(string name)
    {
        var definition = ValidGetDefinition();
        definition["name"] = name;

        await AssertRejected(definition, "name");
    }

    [TestCase(null)]
    [TestCase("/contracts/42")]
    [TestCase("ftp://api.example.com/contracts")]
    [Test(Description = "scenario_TEMP-007-014 - #8")]
    public async Task Given_InvalidUrl_When_UserSavesConnection_Then_SavingIsRejectedWithUrlError(string url)
    {
        var definition = ValidGetDefinition();
        definition["url"] = url;
        definition.Remove("pathParameters");

        await AssertRejected(definition, "url");
    }

    [TestCase("https://{host}/contracts")]
    [TestCase("https://api.example.com/contracts?q={q}")]
    [TestCase("https://api.example.com/contracts/{id")]
    [Test(Description = "scenario_TEMP-007-014 - #9")]
    public async Task Given_PlaceholderOutsidePath_When_UserSavesConnection_Then_SavingIsRejectedWithUrlError(string url)
    {
        var definition = ValidGetDefinition();
        definition["url"] = url;
        definition.Remove("pathParameters");

        await AssertRejected(definition, "url");
    }

    [TestCase("PUT")]
    [TestCase(null)]
    [Test(Description = "scenario_TEMP-007-014 - #10")]
    public async Task Given_UnsupportedMethod_When_UserSavesConnection_Then_SavingIsRejectedWithMethodError(string method)
    {
        var definition = ValidGetDefinition();
        definition["method"] = method;

        await AssertRejected(definition, "method");
    }

    [TestCase("body")]
    [TestCase("bodyParameters")]
    [Test(Description = "scenario_TEMP-007-014 - #11")]
    public async Task Given_GetConnectionWithBodyElement_When_UserSavesConnection_Then_SavingIsRejected(string element)
    {
        var definition = ValidGetDefinition();
        definition[element] = element == "body"
            ? new JsonObject { ["q"] = 1 }
            : new JsonArray(new JsonObject { ["name"] = "limit" });

        await AssertRejected(definition, element);
    }

    [Test(Description = "scenario_TEMP-007-014 - #12")]
    public async Task Given_DefinitionThatIsNotJson_When_UserSavesConnection_Then_SavingIsRejected()
    {
        var content = new StringContent("""{ "name": "Pogoda", """, Encoding.UTF8, "application/json");

        var response = await Client.PostAsync(ConnectionsUrl, content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var connections = await Client.GetFromJsonAsync<JsonArray>(ConnectionsUrl);
        Assert.That(connections, Is.Empty);
    }

    [TestCase("https://api.example.com/contracts/{id}", false, "url")]
    [TestCase("https://api.example.com/contracts", true, "pathParameters[0].name")]
    [Test(Description = "scenario_TEMP-007-014 - #13")]
    public async Task Given_PathParametersNotMatchingUrl_When_UserSavesConnection_Then_SavingIsRejected(
        string url,
        bool declaresId,
        string errorKey)
    {
        var definition = ValidGetDefinition();
        definition["url"] = url;
        if (!declaresId)
        {
            definition.Remove("pathParameters");
        }

        await AssertRejected(definition, errorKey);
    }

    [TestCase("""{"contractId": "{id}"}""", false, "body")]
    [TestCase("""{"contractId": 42}""", true, "bodyParameters[0].name")]
    [Test(Description = "scenario_TEMP-007-014 - #14")]
    public async Task Given_BodyParametersNotMatchingBody_When_UserSavesConnection_Then_SavingIsRejected(
        string body,
        bool declaresId,
        string errorKey)
    {
        var definition = ValidPostDefinition();
        definition["body"] = JsonNode.Parse(body);
        definition["bodyParameters"] = declaresId
            ? new JsonArray(new JsonObject { ["name"] = "id", ["required"] = true })
            : new JsonArray();

        await AssertRejected(definition, errorKey);
    }

    [Test(Description = "scenario_TEMP-007-014 - #15")]
    public async Task Given_PathParameterNameNotInSnakeCase_When_UserSavesConnection_Then_SavingIsRejected()
    {
        var definition = ValidGetDefinition();
        definition["url"] = "https://api.example.com/contracts/{Id}/sellers";
        definition["pathParameters"] = new JsonArray(new JsonObject { ["name"] = "Id", ["required"] = true });

        await AssertRejected(definition, "pathParameters[0].name");
    }

    private static JsonObject ValidPostDefinition()
    {
        return new JsonObject
        {
            ["name"] = "Wyszukiwanie sprzedawców",
            ["method"] = "POST",
            ["url"] = "https://api.example.com/sellers/search",
            ["body"] = new JsonObject { ["contractId"] = "{id}" },
            ["bodyParameters"] = new JsonArray(new JsonObject { ["name"] = "id", ["required"] = true })
        };
    }

    private static JsonObject ValidGetDefinition()
    {
        return new JsonObject
        {
            ["name"] = "Sprzedawcy kontraktu",
            ["method"] = "GET",
            ["url"] = "https://api.example.com/contracts/{id}/sellers",
            ["pathParameters"] = new JsonArray(new JsonObject { ["name"] = "id", ["required"] = true })
        };
    }

    private async Task AssertRejected(JsonObject definition, string errorKey)
    {
        var response = await Client.PostAsJsonAsync(ConnectionsUrl, definition);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().ContainsKey(errorKey), Is.True);
    }
}
