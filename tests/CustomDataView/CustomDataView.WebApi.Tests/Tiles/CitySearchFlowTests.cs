using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace CustomDataView.WebApi.Tests.Tiles;

public class CitySearchFlowTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-041-043 - #22")]
    public async Task Given_SearchWithTabsResultsAndButton_When_UserSearchesPagesAndOpensCity_Then_CityIsShownAsNewRoot()
    {
        Factory.ExternalApi.Respond = (request, _) =>
        {
            if (request.RequestUri!.AbsolutePath == "/cities/1")
            {
                return Task.FromResult(Json("""{"id": 1, "name": "Kraków", "population": 800000}"""));
            }

            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            var page = int.Parse(query["pageNumber"]!);
            var count = page == 1 ? 10 : 2;
            var cities = new JsonArray(Enumerable.Range((page - 1) * 10 + 1, count)
                .Select(id => (JsonNode)new JsonObject { ["id"] = id, ["name"] = id == 1 ? "Kraków" : $"{query["name"]} {id}" })
                .ToArray());
            return Task.FromResult(Json(new JsonObject { ["total"] = 12, ["cities"] = cities }.ToJsonString()));
        };
        var searchConnectionId = await CreateConnection(new
        {
            name = "Szukaj miast",
            method = "GET",
            url = "https://api.example.com/cities",
            queryParameters = new object[]
            {
                new { name = "name", required = false },
                new { name = "country", required = false },
                new { name = "pageNumber", required = true },
                new { name = "pageSize", required = true }
            }
        });
        var cityConnectionId = await CreateConnection(new
        {
            name = "Miasto",
            method = "GET",
            url = "https://api.example.com/cities/{city_id}",
            pathParameters = new object[] { new { name = "city_id", required = true } }
        });
        var cityTileId = await CreateTile(new JsonObject
        {
            ["name"] = "Miasto",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "city", ["kind"] = "connection", ["connectionId"] = cityConnectionId }),
            ["fields"] = new JsonArray(
                new JsonObject { ["source"] = "city", ["path"] = "$.name", ["label"] = "Nazwa" },
                new JsonObject { ["source"] = "city", ["path"] = "$.population", ["label"] = "Ludność" })
        });
        var resultsTileId = await CreateTile(new JsonObject
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
        });
        var nameFormId = await CreateTile(Form("Nazwa miasta", "Miasto"));
        var countryFormId = await CreateTile(Form("Kraj", "Kraj"));
        var filtersId = await CreateTile(new JsonObject
        {
            ["name"] = "Filtry",
            ["display"] = "tabs",
            ["children"] = new JsonArray(
                new JsonObject { ["key"] = "byName", ["label"] = "Po nazwie", ["tileId"] = nameFormId, ["position"] = 0 },
                new JsonObject { ["key"] = "byCountry", ["label"] = "Po kraju", ["tileId"] = countryFormId, ["position"] = 1 })
        });
        var searchId = await CreateTile(new JsonObject
        {
            ["name"] = "Wyszukiwarka miast",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "note", ["kind"] = "input" }),
            ["fields"] = new JsonArray(new JsonObject { ["source"] = "note", ["path"] = "$", ["label"] = "Notatka", ["hidden"] = true }),
            ["children"] = new JsonArray(
                new JsonObject { ["key"] = "filters", ["tileId"] = filtersId, ["position"] = 0 },
                new JsonObject { ["key"] = "results", ["tileId"] = resultsTileId, ["position"] = 1 }),
            ["bindings"] = new JsonArray(
                Binding("filters/byName", "Miasto", "name"),
                Binding("filters/byCountry", "Kraj", "country"))
        });

        var search = await Preview(searchId, new { forms = new Dictionary<string, object> { ["filters/byName"] = new Dictionary<string, object> { ["Miasto"] = "Kra" } } });

        var results = search["children"]![1]!["tile"]!;
        Assert.That(results["items"]!.AsArray(), Has.Count.EqualTo(10), search.ToJsonString());
        Assert.That(results["items"]![0]!["actions"]![0]!["label"]?.GetValue<string>(), Is.EqualTo("Szczegóły"));
        var next = results["pagination"]!["next"]!;
        Assert.That(next["parameters"]!["name"]?.GetValue<string>(), Is.EqualTo("Kra"));

        var secondPage = await Preview(resultsTileId, next.DeepClone());

        Assert.That(secondPage["items"]!.AsArray().Select(item => item!["key"]!.GetValue<int>()), Is.EqualTo(new[] { 11, 12 }));

        var button = results["items"]![0]!["actions"]![0]!;
        var city = await Preview(button["tileId"]!.GetValue<Guid>(), button["inputs"]!.DeepClone());

        Assert.That(city["name"]?.GetValue<string>(), Is.EqualTo("Miasto"));
        Assert.That(city["fields"]![0]!["value"]?.GetValue<string>(), Is.EqualTo("Kraków"));
    }

    private static JsonObject Form(string name, string label)
        => new()
        {
            ["name"] = name,
            ["display"] = "form",
            ["fields"] = new JsonArray(new JsonObject { ["label"] = label, ["type"] = "text" })
        };

    private static JsonObject Binding(string from, string field, string parameter)
        => new()
        {
            ["from"] = from,
            ["to"] = "results",
            ["map"] = new JsonArray(new JsonObject { ["field"] = field, ["parameter"] = parameter })
        };

    private async Task<JsonObject> Preview(Guid id, object values)
    {
        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", values);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return await response.Content.ReadFromJsonAsync<JsonObject>();
    }
}
