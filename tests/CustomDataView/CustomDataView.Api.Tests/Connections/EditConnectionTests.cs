using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Connections;

public class EditConnectionTests : ConnectionApiTests
{
    [Test(Description = "scenario_TEMP-007-014 - #24")]
    public async Task Given_GetConnectionOnSecondPosition_When_UserReplacesItWithPostConnection_Then_OnlyNewDefinitionIsShown()
    {
        await CreateConnection(new { name = "Pogoda", method = "GET", url = "https://api.example.com/weather" });
        var id = await CreateConnection(new
        {
            name = "Sprzedawcy kontraktu",
            method = "GET",
            url = "https://api.example.com/contracts/{id}/sellers",
            pathParameters = new[] { new { name = "id", required = true } }
        });

        var response = await Client.PutAsJsonAsync($"{ConnectionsUrl}/{id}", new
        {
            name = "Wyszukiwanie sprzedawców",
            method = "POST",
            url = "https://api.example.com/sellers/search",
            body = new { contractId = "{id}" },
            bodyParameters = new[] { new { name = "id", required = true } }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var details = await Client.GetFromJsonAsync<JsonObject>($"{ConnectionsUrl}/{id}");
        Assert.That(details["name"]?.GetValue<string>(), Is.EqualTo("Wyszukiwanie sprzedawców"));
    }
}
