using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class TileActionTests : TileApiTests
{
    private const string Dir = "scenario_TEMP-041-043";

    private Guid searchConnectionId;
    private Guid cityConnectionId;
    private Guid cityTileId;

    [SetUp]
    public async Task CreateTiles()
    {
        Factory.ExternalApi.Respond = (request, _) => Task.FromResult(request.RequestUri!.AbsolutePath switch
        {
            "/cities" => Json("""{"total": 2, "cities": [{"id": 1, "name": "Kraków"}, {"id": 2, "name": "Krynica"}]}"""),
            "/cities/1" => Json("""{"id": 1, "name": "Kraków", "population": 800000}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        searchConnectionId = await CreateConnection(new
        {
            name = "Szukaj miast",
            method = "GET",
            url = "https://api.example.com/cities",
            queryParameters = new object[]
            {
                new { name = "name", required = false },
                new { name = "pageNumber", required = false },
                new { name = "pageSize", required = false }
            }
        });
        cityConnectionId = await CreateConnection(new
        {
            name = "Miasto",
            method = "GET",
            url = "https://api.example.com/cities/{city_id}",
            pathParameters = new object[] { new { name = "city_id", required = true } }
        });
        cityTileId = await CreateTile(City());
    }

    [Test(Description = $"{Dir} - #16")]
    public async Task Given_ItemButtonToCity_When_UserPreviewsResults_Then_EachItemHasButtonWithItsCityId()
    {
        var id = await CreateTile(Results());

        var preview = await Preview(id, new { parameters = new { name = "Kra" } });

        var items = preview["items"]!.AsArray();
        Assert.That(
            JsonNode.DeepEquals(
                items[0]!["actions"],
                JsonNode.Parse("""[{"key": "details", "label": "Szczegóły", "type": "button", "tileId": "TILE", "inputs": {"parameters": {"city_id": 1}}}]"""
                    .Replace("TILE", cityTileId.ToString()))),
            Is.True,
            items[0]!.ToJsonString());
        Assert.That(items[1]!["actions"]![0]!["inputs"]!["parameters"]!["city_id"]?.GetValue<int>(), Is.EqualTo(2));
    }

    [Test(Description = $"{Dir} - #17")]
    public async Task Given_ButtonInputs_When_FrontPreviewsTargetTile_Then_TargetLoadsCity()
    {
        var id = await CreateTile(Results());
        var action = (await Preview(id, new { }))["items"]![0]!["actions"]![0]!;

        var city = await Preview(action["tileId"]!.GetValue<Guid>(), action["inputs"]!.DeepClone());

        Assert.That(Factory.ExternalApi.Requests[^1].Url, Is.EqualTo("https://api.example.com/cities/1"));
        Assert.That(city["fields"]![0]!["value"]?.GetValue<string>(), Is.EqualTo("Kraków"));
    }

    [Test(Description = $"{Dir} - #18")]
    public async Task Given_TileLinkToResults_When_UserPreviewsCity_Then_LinkCarriesCityName()
    {
        var resultsId = await CreateTile(Results());
        var city = City();
        city["actions"] = new JsonArray(new JsonObject
        {
            ["key"] = "all",
            ["label"] = "Wszystkie miasta",
            ["type"] = "link",
            ["tileId"] = resultsId,
            ["map"] = new JsonArray(new JsonObject { ["field"] = "Nazwa", ["parameter"] = "name" })
        });
        await Client.PutAsJsonAsync($"{TilesUrl}/{cityTileId}", city);

        var preview = await Preview(cityTileId, new { parameters = new { city_id = "1" } });

        Assert.That(
            JsonNode.DeepEquals(
                preview["actions"],
                JsonNode.Parse("""[{"key": "all", "label": "Wszystkie miasta", "type": "link", "tileId": "TILE", "inputs": {"parameters": {"name": "Kraków"}}}]"""
                    .Replace("TILE", resultsId.ToString()))),
            Is.True,
            preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #19")]
    public async Task Given_FailingFieldOrChangedTarget_When_UserPreviewsResults_Then_ButtonHasError()
    {
        var results = Results();
        results["fields"]!.AsArray().Add(new JsonObject { ["source"] = "cities", ["path"] = "$.code", ["label"] = "Kod" });
        results["actions"]![0]!["map"]![0]!["field"] = "Kod";
        var id = await CreateTile(results);

        var failed = (await Preview(id, new { }))["items"]![0]!["actions"]![0]!;

        Assert.That(
            JsonNode.DeepEquals(
                failed["error"],
                JsonNode.Parse("""{"code": "dependency_failed", "cause": {"node": "cities", "field": "Kod", "code": "path_not_found"}}""")),
            Is.True,
            failed.ToJsonString());
        Assert.That(failed["inputs"], Is.Null);

        var city = City();
        city["sources"] = new JsonArray(new JsonObject { ["key"] = "city", ["kind"] = "connection", ["connectionId"] = searchConnectionId });
        await Client.PutAsJsonAsync($"{TilesUrl}/{cityTileId}", city);
        results["actions"]![0]!["map"]![0]!["field"] = "ID";
        await Client.PutAsJsonAsync($"{TilesUrl}/{id}", results);

        var invalid = (await Preview(id, new { }))["items"]![0]!["actions"]![0]!;

        Assert.That(invalid["error"]!["code"]?.GetValue<string>(), Is.EqualTo("invalid_binding"), invalid.ToJsonString());
        Assert.That(invalid["error"]!["parameters"]![0]!["name"]?.GetValue<string>(), Is.EqualTo("city_id"));
    }

    [TestCase("missing-target", "actions[0].tileId")]
    [TestCase("missing-field", "actions[0].map[0].field")]
    [TestCase("parameter-outside-target", "actions[0].map[0].parameter")]
    [TestCase("unknown-type", "actions[0].type")]
    [TestCase("item-scope-without-items", "actions[0].scope")]
    [TestCase("in-form", "actions")]
    [TestCase("duplicate-key", "actions[1].key")]
    [TestCase("no-label", "actions[0].label")]
    [Test(Description = $"{Dir} - #20")]
    public async Task Given_InvalidAction_When_UserSavesTile_Then_SaveIsRejected(string invalid, string errorKey)
    {
        var tile = Results();
        var action = tile["actions"]![0]!.AsObject();
        switch (invalid)
        {
            case "missing-target":
                action["tileId"] = Guid.NewGuid();
                break;
            case "missing-field":
                action["map"]![0]!["field"] = "Ulica";
                break;
            case "parameter-outside-target":
                action["map"]![0]!["parameter"] = "region";
                break;
            case "unknown-type":
                action["type"] = "menu";
                break;
            case "item-scope-without-items":
                tile = City();
                tile["actions"] = new JsonArray(new JsonObject
                {
                    ["key"] = "self",
                    ["label"] = "Odśwież",
                    ["tileId"] = cityTileId,
                    ["scope"] = "item",
                    ["map"] = new JsonArray()
                });
                break;
            case "in-form":
                tile = new JsonObject
                {
                    ["name"] = "Formularz",
                    ["display"] = "form",
                    ["fields"] = new JsonArray(new JsonObject { ["label"] = "Miasto", ["type"] = "text" }),
                    ["actions"] = new JsonArray(new JsonObject { ["key"] = "go", ["label"] = "Idź", ["tileId"] = cityTileId })
                };
                break;
            case "duplicate-key":
                tile["actions"]!.AsArray().Add(action.DeepClone());
                break;
            case "no-label":
                action.Remove("label");
                break;
        }

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Does.Contain(errorKey), problem.ToJsonString());
    }

    [Test(Description = $"{Dir} - #21")]
    public async Task Given_ResultsWithButtonToCity_When_UserDeletesCity_Then_DeleteIsBlockedWithResults()
    {
        var resultsId = await CreateTile(Results());

        var response = await Client.DeleteAsync($"{TilesUrl}/{cityTileId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["code"]?.GetValue<string>(), Is.EqualTo("tile_in_use"), problem.ToJsonString());
        Assert.That(problem["tiles"]!.AsArray().Select(tile => tile!["id"]!.GetValue<Guid>()), Is.EqualTo(new[] { resultsId }));
    }

    private JsonObject Results()
        => new()
        {
            ["name"] = "Wyniki",
            ["display"] = "pagination_table",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "cities", ["kind"] = "connection", ["connectionId"] = searchConnectionId }),
            ["items"] = new JsonObject { ["source"] = "cities", ["path"] = "$.cities[*]", ["key"] = "ID" },
            ["pagination"] = new JsonObject { ["pageSize"] = 10, ["totalPath"] = "$.total" },
            ["fields"] = new JsonArray(
                new JsonObject { ["source"] = "cities", ["path"] = "$.id", ["label"] = "ID" },
                new JsonObject { ["source"] = "cities", ["path"] = "$.name", ["label"] = "Miasto" }),
            ["actions"] = new JsonArray(new JsonObject
            {
                ["key"] = "details",
                ["label"] = "Szczegóły",
                ["tileId"] = cityTileId,
                ["map"] = new JsonArray(new JsonObject { ["field"] = "ID", ["parameter"] = "city_id" })
            })
        };

    private JsonObject City()
        => new()
        {
            ["name"] = "Miasto",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "city", ["kind"] = "connection", ["connectionId"] = cityConnectionId }),
            ["fields"] = new JsonArray(
                new JsonObject { ["source"] = "city", ["path"] = "$.name", ["label"] = "Nazwa" },
                new JsonObject { ["source"] = "city", ["path"] = "$.population", ["label"] = "Ludność" })
        };

    private async Task<JsonObject> Preview(Guid id, object values)
    {
        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", values);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonObject>();
    }
}
