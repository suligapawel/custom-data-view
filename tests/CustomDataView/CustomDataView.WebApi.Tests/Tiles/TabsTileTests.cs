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

    [TestCase("no-tabs", "children")]
    [TestCase("tab-without-title", "children[0].label")]
    [TestCase("item-tab", "children[0].scope")]
    [TestCase("own-fields", "fields")]
    [TestCase("own-sources", "sources")]
    [Test(Description = $"{Dir} - #5")]
    public async Task Given_InvalidTabsTile_When_UserSavesIt_Then_SaveIsRejected(string invalid, string errorKey)
    {
        var tile = Filters();
        switch (invalid)
        {
            case "no-tabs":
                tile["children"] = new JsonArray();
                break;
            case "tab-without-title":
                tile["children"]![0]!.AsObject().Remove("label");
                break;
            case "item-tab":
                tile["children"]![0]!["scope"] = "item";
                break;
            case "own-fields":
                tile["fields"] = new JsonArray(new JsonObject { ["source"] = "note", ["path"] = "$", ["label"] = "Notatka" });
                break;
            case "own-sources":
                tile["sources"] = new JsonArray(new JsonObject { ["key"] = "note", ["kind"] = "input" });
                break;
        }

        await AssertRejected(tile, errorKey);
    }

    [TestCase("filters/missing", "Miasto", "bindings[0].from")]
    [TestCase("filters/byName", "Ulica", "bindings[0].map[0].field")]
    [TestCase("results/byName", "Miasto", "bindings[0].from", "filters/byName")]
    [TestCase("filters/byName", "Miasto", "bindings[0].to", "filters")]
    [Test(Description = $"{Dir} - #6")]
    public async Task Given_BindingFromInvalidDescendantPath_When_UserSavesSearch_Then_SaveIsRejected(
        string from,
        string field,
        string errorKey,
        string to = "results")
    {
        var search = Search(await CreateTile(Filters(lazyCountry: false)));
        search["bindings"]![0] = Binding(from, to, field, "name");

        await AssertRejected(search, errorKey);
    }

    [Test(Description = $"{Dir} - #7")]
    public async Task Given_ChainOfFourLevels_When_UserSavesTopTileAndThenCreatesCycle_Then_TopIsSavedAndCycleIsRejected()
    {
        var filtersId = await CreateTile(Filters());
        var searchId = await CreateTile(Search(filtersId));
        var top = new JsonObject
        {
            ["name"] = "Pulpit",
            ["display"] = "tabs",
            ["children"] = new JsonArray(new JsonObject { ["key"] = "search", ["label"] = "Szukaj", ["tileId"] = searchId, ["position"] = 0 })
        };

        var saved = await Client.PostAsJsonAsync(TilesUrl, top);

        Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.Created), await saved.Content.ReadAsStringAsync());
        var topId = (await saved.Content.ReadFromJsonAsync<CreatedTile>()).Id;
        var cycle = Filters();
        cycle["children"]!.AsArray().Add(new JsonObject { ["key"] = "top", ["label"] = "Pulpit", ["tileId"] = topId, ["position"] = 2 });
        var response = await Client.PutAsJsonAsync($"{TilesUrl}/{filtersId}", cycle);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Does.Contain("children[2].tileId"), problem.ToJsonString());
    }

    [Test(Description = $"{Dir} - #8")]
    public async Task Given_AncestorBindingUsesFormFieldThroughPath_When_UserRemovesField_Then_EditIsBlocked()
    {
        var searchId = await CreateTile(Search(await CreateTile(Filters(lazyCountry: false))));

        var response = await Client.PutAsJsonAsync($"{TilesUrl}/{nameFormId}", Form("Nazwa miasta", "Ulica", false));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["fields"]!.AsArray().Select(field => field!.GetValue<string>()), Is.EqualTo(new[] { "Miasto" }), problem.ToJsonString());
        Assert.That(problem["tiles"]!.AsArray().Select(tile => tile!["id"]!.GetValue<Guid>()), Is.EqualTo(new[] { searchId }), problem.ToJsonString());
    }

    private async Task AssertRejected(JsonObject tile, string errorKey)
    {
        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Does.Contain(errorKey), problem.ToJsonString());
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
