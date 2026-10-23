using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class TabsTileTests : TileApiTests
{
    private const string Dir = "scenario_TEMP-041-043";

    private Guid searchConnectionId;
    private Guid nameFormId;
    private Guid countryFormId;
    private Guid resultsTileId;

    [SetUp]
    public async Task CreateTiles()
    {
        Factory.ExternalApi.Respond = (request, _) => Task.FromResult(request.RequestUri!.AbsoluteUri.StartsWith("https://api.example.com/cities")
            ? Json("""{"cities": [{"id": 1, "name": "Kraków"}]}""")
            : new HttpResponseMessage(HttpStatusCode.NotFound));
        searchConnectionId = await CreateConnection(new
        {
            name = "Szukaj miast",
            method = "GET",
            url = "https://api.example.com/cities",
            queryParameters = new object[] { new { name = "name", required = false }, new { name = "country", required = false } }
        });
        nameFormId = await CreateTile(Form("Nazwa miasta", "Miasto", false));
        countryFormId = await CreateTile(Form("Kraj", "Kraj", false));
        resultsTileId = await CreateTile(new JsonObject
        {
            ["name"] = "Wyniki",
            ["display"] = "table",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "cities", ["kind"] = "connection", ["connectionId"] = searchConnectionId }),
            ["items"] = new JsonObject { ["source"] = "cities", ["path"] = "$.cities[*]", ["key"] = "ID" },
            ["fields"] = new JsonArray(
                new JsonObject { ["source"] = "cities", ["path"] = "$.id", ["label"] = "ID" },
                new JsonObject { ["source"] = "cities", ["path"] = "$.name", ["label"] = "Miasto" })
        });
    }

    [Test(Description = $"{Dir} - #1")]
    public async Task Given_TabsWithEagerAndLazyTab_When_UserPreviewsTabs_Then_TabsAreReturnedWithTitlesInOrder()
    {
        var id = await CreateTile(Filters());

        var preview = await Preview(id, new { });

        Assert.That(preview["display"]?.GetValue<string>(), Is.EqualTo("tabs"));
        var tabs = preview["children"]!.AsArray();
        Assert.That(tabs.Select(tab => tab!["label"]?.GetValue<string>()), Is.EqualTo(new[] { "Po nazwie", "Po kraju" }));
        Assert.That(tabs[0]!["state"]?.GetValue<string>(), Is.EqualTo("data"));
        Assert.That(tabs[0]!["tile"]!["fields"]![0]!["label"]?.GetValue<string>(), Is.EqualTo("Miasto"));
        Assert.That(tabs[1]!["state"]?.GetValue<string>(), Is.EqualTo("awaiting_load"));
    }

    private JsonObject Filters()
        => new()
        {
            ["name"] = "Filtry",
            ["display"] = "tabs",
            ["children"] = new JsonArray(
                new JsonObject { ["key"] = "byName", ["label"] = "Po nazwie", ["tileId"] = nameFormId, ["position"] = 0 },
                new JsonObject { ["key"] = "byCountry", ["label"] = "Po kraju", ["tileId"] = countryFormId, ["position"] = 1, ["load"] = "lazy" })
        };

    private static JsonObject Form(string name, string label, bool required)
        => new()
        {
            ["name"] = name,
            ["display"] = "form",
            ["fields"] = new JsonArray(new JsonObject { ["label"] = label, ["type"] = "text", ["required"] = required })
        };

    private async Task<JsonObject> Preview(Guid id, object values)
    {
        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", values);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonObject>();
    }
}
