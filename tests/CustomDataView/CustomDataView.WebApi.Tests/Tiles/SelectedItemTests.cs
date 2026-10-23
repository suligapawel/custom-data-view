using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class SelectedItemTests : TileApiTests
{
    private Guid citiesTileId;
    private Guid weatherTileId;
    private Guid unitsTileId;

    [SetUp]
    public async Task CreateTiles()
    {
        Factory.ExternalApi.Respond = (request, _) => Task.FromResult(request.RequestUri!.AbsoluteUri switch
        {
            "https://api.example.com/cities" => Json("""{"cities": [{"id": 1, "name": "Kraków"}, {"id": 2, "name": "Gdańsk"}]}"""),
            "https://api.example.com/weather?cityId=2&units=metric" => Json("""{"temp": 14}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        var citiesConnectionId = await CreateConnection(new { name = "Miasta", method = "GET", url = "https://api.example.com/cities" });
        var weatherConnectionId = await CreateConnection(new
        {
            name = "Pogoda",
            method = "GET",
            url = "https://api.example.com/weather",
            queryParameters = new object[] { new { name = "cityId", required = true }, new { name = "units", required = true } }
        });
        citiesTileId = await CreateTile(new JsonObject
        {
            ["name"] = "Miasta",
            ["display"] = "table",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "cities", ["kind"] = "connection", ["connectionId"] = citiesConnectionId }),
            ["items"] = new JsonObject { ["source"] = "cities", ["path"] = "$.cities[*]", ["key"] = "ID" },
            ["fields"] = new JsonArray(
                new JsonObject { ["source"] = "cities", ["path"] = "$.id", ["label"] = "ID" },
                new JsonObject { ["source"] = "cities", ["path"] = "$.name", ["label"] = "Miasto" })
        });
        weatherTileId = await CreateTile(new JsonObject
        {
            ["name"] = "Pogoda",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "weather", ["kind"] = "connection", ["connectionId"] = weatherConnectionId }),
            ["fields"] = new JsonArray(new JsonObject { ["source"] = "weather", ["path"] = "$.temp", ["label"] = "Temperatura" })
        });
        unitsTileId = await CreateTile(new JsonObject
        {
            ["name"] = "Jednostki",
            ["display"] = "form",
            ["fields"] = new JsonArray(new JsonObject { ["label"] = "Jednostki", ["type"] = "text", ["default"] = "metric" })
        });
    }

    [Test(Description = "scenario_TEMP-039 - #1")]
    public async Task Given_SelectedBindingToWeather_When_UserPreviewsTile_Then_WeatherAwaitsInputWithPendingAndOtherInputs()
    {
        var id = await CreateTile(CitiesAndWeather("selected"));

        var preview = await Preview(id, new { });

        var weather = preview["children"]![2]!;
        Assert.That(weather["state"]?.GetValue<string>(), Is.EqualTo("awaiting_input"));
        Assert.That(
            JsonNode.DeepEquals(weather["pending"], JsonNode.Parse("""[{"from": "cities", "field": "ID", "scope": "selected", "parameter": "cityId"}]""")),
            Is.True,
            weather.ToJsonString());
        Assert.That(JsonNode.DeepEquals(weather["inputs"], JsonNode.Parse("""{"parameters": {"units": "metric"}}""")), Is.True, weather.ToJsonString());
        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url), Is.EqualTo(new[] { "https://api.example.com/cities" }));
    }

    [Test(Description = "scenario_TEMP-039 - #2")]
    public async Task Given_InputsFromAwaitingWeather_When_FrontPreviewsWeatherWithSelectedCity_Then_WeatherIsLoaded()
    {
        var id = await CreateTile(CitiesAndWeather("selected"));
        var inputs = (await Preview(id, new { }))["children"]![2]!["inputs"]!.AsObject();
        inputs["parameters"]!["cityId"] = 2;

        var weather = await Preview(weatherTileId, inputs);

        Assert.That(weather["fields"]![0]!["value"]?.GetValue<int>(), Is.EqualTo(14));
    }

    [Test(Description = "scenario_TEMP-039 - #3")]
    public async Task Given_BindingFromItemsWithoutScope_When_UserPreviewsTile_Then_ScopeDefaultsToSelected()
    {
        var id = await CreateTile(CitiesAndWeather(null));

        var preview = await Preview(id, new { });

        var weather = preview["children"]![2]!;
        Assert.That(weather["state"]?.GetValue<string>(), Is.EqualTo("awaiting_input"));
        Assert.That(weather["pending"]![0]!["scope"]?.GetValue<string>(), Is.EqualTo("selected"));
    }

    [Test(Description = "scenario_TEMP-039 - #4")]
    public async Task Given_SelectedScopeFromNodeWithoutItems_When_UserSavesTile_Then_SaveIsRejectedWithScopeError()
    {
        var tile = CitiesAndWeather("selected");
        tile["bindings"]![1]!["map"]![0]!["scope"] = "selected";

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Does.Contain("bindings[1].map[0].scope"), problem.ToJsonString());
    }

    private JsonObject CitiesAndWeather(string scope)
        => new()
        {
            ["name"] = "Miasta i pogoda",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "note", ["kind"] = "input" }),
            ["fields"] = new JsonArray(new JsonObject { ["source"] = "note", ["path"] = "$", ["label"] = "Notatka", ["hidden"] = true }),
            ["children"] = new JsonArray(
                new JsonObject { ["key"] = "cities", ["tileId"] = citiesTileId, ["position"] = 0 },
                new JsonObject { ["key"] = "units", ["tileId"] = unitsTileId, ["position"] = 1 },
                new JsonObject { ["key"] = "weather", ["tileId"] = weatherTileId, ["position"] = 2 }),
            ["bindings"] = new JsonArray(
                new JsonObject
                {
                    ["from"] = "cities",
                    ["to"] = "weather",
                    ["map"] = new JsonArray(new JsonObject { ["field"] = "ID", ["parameter"] = "cityId", ["scope"] = scope })
                },
                new JsonObject
                {
                    ["from"] = "units",
                    ["to"] = "weather",
                    ["map"] = new JsonArray(new JsonObject { ["field"] = "Jednostki", ["parameter"] = "units" })
                })
        };

    private async Task<JsonObject> Preview(Guid id, object values)
        => await (await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", values)).Content.ReadFromJsonAsync<JsonObject>();
}
