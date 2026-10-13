using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Connections;

public class ConnectionValidationTests : ConnectionApiTests
{
    [TestCase(null)]
    [Test(Description = "scenario_TEMP-007-014 - #7")]
    public async Task Given_InvalidName_When_UserSavesConnection_Then_SavingIsRejectedWithNameError(string name)
    {
        var definition = ValidGetDefinition();
        definition["name"] = name;

        await AssertRejected(definition, "name");
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
