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

    [Test(Description = $"{Dir} - #2")]
    public async Task Given_BindingFromFormInTab_When_UserFillsFormAndPreviewsSearch_Then_ResultsAreQueriedWithFormValue()
    {
        var id = await CreateTile(Search(await CreateTile(Filters(lazyCountry: false))));

        var preview = await Preview(id, new { forms = new Dictionary<string, object> { ["filters/byName"] = new Dictionary<string, object> { ["Miasto"] = "Kra" } } });

        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url), Is.EqualTo(new[] { "https://api.example.com/cities?name=Kra" }));
        var results = preview["children"]![1]!;
        Assert.That(results["state"]?.GetValue<string>(), Is.EqualTo("data"), results.ToJsonString());
        Assert.That(results["tile"]!["items"]![0]!["key"]?.GetValue<int>(), Is.EqualTo(1));
    }

    [Test(Description = $"{Dir} - #3")]
    public async Task Given_RequiredFormFieldInTab_When_UserPreviewsSearchWithoutCity_Then_ResultsAwaitFormValue()
    {
        await Client.PutAsJsonAsync($"{TilesUrl}/{nameFormId}", Form("Nazwa miasta", "Miasto", true));
        var id = await CreateTile(Search(await CreateTile(Filters(lazyCountry: false))));

        var preview = await Preview(id, new { });

        var results = preview["children"]![1]!;
        Assert.That(results["state"]?.GetValue<string>(), Is.EqualTo("awaiting_input"), results.ToJsonString());
        Assert.That(
            JsonNode.DeepEquals(results["pending"], JsonNode.Parse("""[{"from": "filters/byName", "field": "Miasto", "parameter": "name"}]""")),
            Is.True,
            results.ToJsonString());
        Assert.That(Factory.ExternalApi.Requests, Is.Empty);
    }

    [Test(Description = $"{Dir} - #4")]
    public async Task Given_TabsFedByParentAndByOtherTab_When_UserPreviewsSearch_Then_TabsAreLoadedWithDeliveredValues()
    {
        var weatherConnectionId = await CreateConnection(new
        {
            name = "Pogoda",
            method = "GET",
            url = "https://api.example.com/cities/weather",
            queryParameters = new object[] { new { name = "cityId", required = true } }
        });
        var weatherTileId = await CreateTile(new JsonObject
        {
            ["name"] = "Pogoda",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "weather", ["kind"] = "connection", ["connectionId"] = weatherConnectionId }),
            ["fields"] = new JsonArray(new JsonObject { ["source"] = "weather", ["path"] = "$", ["label"] = "Pogoda" })
        });
        var filters = Filters(lazyCountry: false);
        filters["children"]!.AsArray().Add(new JsonObject { ["key"] = "weather", ["label"] = "Pogoda", ["tileId"] = weatherTileId, ["position"] = 2 });
        filters["children"]!.AsArray().Add(new JsonObject { ["key"] = "byNameWeather", ["label"] = "Pogoda miasta", ["tileId"] = weatherTileId, ["position"] = 3 });
        filters["bindings"] = new JsonArray(Binding("byName", "byNameWeather", "Miasto", "cityId"));
        var search = Search(await CreateTile(filters));
        search["fields"] = new JsonArray(new JsonObject { ["source"] = "note", ["path"] = "$.city", ["label"] = "Domyślne miasto", ["hidden"] = true });
        search["bindings"]!.AsArray().Add(Binding("parent", "filters/weather", "Domyślne miasto", "cityId"));
        var id = await CreateTile(search);

        var preview = await Preview(id, new
        {
            inputs = new { note = new { city = "7" } },
            forms = new Dictionary<string, object> { ["filters/byName"] = new Dictionary<string, object> { ["Miasto"] = "9" } }
        });

        var tabs = preview["children"]![0]!["tile"]!["children"]!.AsArray();
        Assert.That(tabs[2]!["state"]?.GetValue<string>(), Is.EqualTo("data"), tabs[2]!.ToJsonString());
        Assert.That(tabs[3]!["state"]?.GetValue<string>(), Is.EqualTo("data"), tabs[3]!.ToJsonString());
        Assert.That(
            Factory.ExternalApi.Requests.Select(request => request.Url),
            Is.SupersetOf(new[] { "https://api.example.com/cities/weather?cityId=7", "https://api.example.com/cities/weather?cityId=9" }));
    }

    private JsonObject Search(Guid filtersId)
        => new()
        {
            ["name"] = "Wyszukiwarka miast",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "note", ["kind"] = "input" }),
            ["fields"] = new JsonArray(new JsonObject { ["source"] = "note", ["path"] = "$", ["label"] = "Notatka", ["hidden"] = true }),
            ["children"] = new JsonArray(
                new JsonObject { ["key"] = "filters", ["tileId"] = filtersId, ["position"] = 0 },
                new JsonObject { ["key"] = "results", ["tileId"] = resultsTileId, ["position"] = 1 }),
            ["bindings"] = new JsonArray(
                Binding("filters/byName", "results", "Miasto", "name"),
                Binding("filters/byCountry", "results", "Kraj", "country"))
        };

    private static JsonObject Binding(string from, string to, string field, string parameter)
        => new()
        {
            ["from"] = from,
            ["to"] = to,
            ["map"] = new JsonArray(new JsonObject { ["field"] = field, ["parameter"] = parameter })
        };

    private JsonObject Filters(bool lazyCountry = true)
        => new()
        {
            ["name"] = "Filtry",
            ["display"] = "tabs",
            ["children"] = new JsonArray(
                new JsonObject { ["key"] = "byName", ["label"] = "Po nazwie", ["tileId"] = nameFormId, ["position"] = 0 },
                new JsonObject { ["key"] = "byCountry", ["label"] = "Po kraju", ["tileId"] = countryFormId, ["position"] = 1, ["load"] = lazyCountry ? "lazy" : "eager" })
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
