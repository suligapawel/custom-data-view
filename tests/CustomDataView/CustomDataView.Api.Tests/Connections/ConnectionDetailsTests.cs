using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Connections;

public class ConnectionDetailsTests : ConnectionApiTests
{
    [Test(Description = "scenario_TEMP-007-014 - #22")]
    public async Task Given_PostConnectionWithParameters_When_UserBrowsesDetails_Then_FullDefinitionIsShown()
    {
        var id = await CreateConnection(new
        {
            name = "Wyszukiwanie sprzedawców",
            method = "POST",
            url = "https://api.example.com/contracts/{contract_id}/sellers/search",
            headers = new[] { new { name = "Accept-Language", value = "pl" } },
            pathParameters = new[] { new { name = "contract_id", required = true } },
            queryParameters = new[] { new { name = "pageSize", required = false, defaultValue = "20" } },
            body = new { contractId = "{id}", limit = "{limit}" },
            bodyParameters = new object[]
            {
                new { name = "id", required = true },
                new { name = "limit", required = false, defaultValue = 10 }
            }
        });

        var details = await Client.GetFromJsonAsync<JsonObject>($"{ConnectionsUrl}/{id}");

        Assert.That(details["name"]?.GetValue<string>(), Is.EqualTo("Wyszukiwanie sprzedawców"));
        Assert.That(details["method"]?.GetValue<string>(), Is.EqualTo("POST"));
        Assert.That(details["url"]?.GetValue<string>(), Is.EqualTo("https://api.example.com/contracts/{contract_id}/sellers/search"));
        Assert.That(
            JsonNode.DeepEquals(details["headers"], JsonNode.Parse("""[{"name": "Accept-Language", "value": "pl"}]""")),
            Is.True);
        Assert.That(
            JsonNode.DeepEquals(
                details["pathParameters"],
                JsonNode.Parse("""[{"name": "contract_id", "required": true, "defaultValue": null}]""")),
            Is.True);
        Assert.That(
            JsonNode.DeepEquals(
                details["queryParameters"],
                JsonNode.Parse("""[{"name": "pageSize", "required": false, "defaultValue": "20"}]""")),
            Is.True);
        Assert.That(
            JsonNode.DeepEquals(
                details["bodyParameters"],
                JsonNode.Parse("""
                    [{"name": "id", "required": true, "defaultValue": null},
                     {"name": "limit", "required": false, "defaultValue": 10}]
                    """)),
            Is.True);
        Assert.That(
            JsonNode.DeepEquals(details["body"], JsonNode.Parse("""{"contractId": "{id}", "limit": "{limit}"}""")),
            Is.True);
    }
}
