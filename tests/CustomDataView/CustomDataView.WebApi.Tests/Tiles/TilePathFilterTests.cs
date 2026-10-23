using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class TilePathFilterTests : TileApiTests
{
    private const string CitiesUrl = "https://api.example.com/cities";

    private Guid citiesConnectionId;

    [SetUp]
    public async Task CreateCitiesConnection()
    {
        var respond = Factory.ExternalApi.Respond;
        Factory.ExternalApi.Respond = (request, cancellationToken) => request.RequestUri!.AbsoluteUri == CitiesUrl
            ? Task.FromResult(Json("""
                                   {"cities": [{"id": 1, "name": "Kraków", "example": {"_type": "capital"}},
                                               {"id": 2, "name": "Gdańsk", "example": {"_type": "port"}},
                                               {"id": 3, "name": "Sopot"}]}
                                   """))
            : respond(request, cancellationToken);
        citiesConnectionId = await CreateConnection(new { name = "Miasta", method = "GET", url = CitiesUrl });
    }

    [Test(Description = "scenario_TEMP-040 - #1")]
    public async Task Given_FieldWithFilterPath_When_UserPreviewsTile_Then_FieldHasValuesOfMatchingElements()
    {
        var id = await CreateTile(Tile("Porty", Field(citiesConnectionId, "$.cities[?@.example._type == 'port'].name", "Miasto")));

        var preview = await Preview(id);

        Assert.That(JsonNode.DeepEquals(preview["fields"]![0]!["value"], JsonNode.Parse("""["Gdańsk"]""")), Is.True, preview.ToJsonString());
    }

    [Test(Description = "scenario_TEMP-040 - #2")]
    public async Task Given_TableWithFilteredItems_When_UserPreviewsIt_Then_ItemsWithoutValueSatisfyNotEqual()
    {
        var id = await CreateTile(CitiesTable("$.cities[?(@.example._type != 'capital')]"));

        var preview = await Preview(id);

        Assert.That(preview["items"]!.AsArray().Select(item => item!["key"]!.GetValue<int>()), Is.EqualTo(new[] { 2, 3 }), preview.ToJsonString());
        Assert.That(preview["items"]![1]!["fields"]![1]!["value"]!.GetValue<string>(), Is.EqualTo("Sopot"));
    }

    [TestCase("$.cities[?@.id == 2].name", """["Gdańsk"]""")]
    [TestCase("$.cities[?@.id == 2.0].name", """["Gdańsk"]""")]
    [TestCase("$.cities[?@.id == '2'].name", "[]")]
    [TestCase("$.cities[?@.id != 1].name", """["Gdańsk","Sopot"]""")]
    [TestCase("$.cities[?@.example == null].name", "[]")]
    [TestCase("$.cities[?@.missing == true].name", "[]")]
    [Test(Description = "scenario_TEMP-040 - #3")]
    public async Task Given_FieldWithFilterLiteral_When_UserPreviewsTile_Then_ValuesAreComparedByType(string path, string expected)
    {
        var id = await CreateTile(Tile("Miasto", Field(citiesConnectionId, path, "Miasto")));

        var preview = await Preview(id);

        Assert.That(JsonNode.DeepEquals(preview["fields"]![0]!["value"], JsonNode.Parse(expected)), Is.True, preview.ToJsonString());
    }

    [TestCase("$.cities[?@.id]")]
    [TestCase("$.cities[?@.id > 1]")]
    [TestCase("$.cities[?@.name == 'Gda]")]
    [TestCase("$.cities[?@.name == Gdańsk]")]
    [TestCase("$.cities[?@.* == 1]")]
    [TestCase("$.cities[?(@.id == 1]")]
    [TestCase("$.cities[?$.id == 1]")]
    [Test(Description = "scenario_TEMP-040 - #4")]
    public async Task Given_FieldWithInvalidFilter_When_UserSavesTile_Then_SaveIsRejectedWithFirstPathError(string path)
    {
        var tile = Tile("Miasto", Field(citiesConnectionId, path, "Miasto"));

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Does.Contain("fields[0].path"), problem.ToJsonString());
    }

    private JsonObject CitiesTable(string itemsPath)
        => new()
        {
            ["name"] = "Miasta",
            ["display"] = "table",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "cities", ["kind"] = "connection", ["connectionId"] = citiesConnectionId }),
            ["items"] = new JsonObject { ["source"] = "cities", ["path"] = itemsPath, ["key"] = "ID" },
            ["fields"] = new JsonArray(
                new JsonObject { ["source"] = "cities", ["path"] = "$.id", ["label"] = "ID" },
                new JsonObject { ["source"] = "cities", ["path"] = "$.name", ["label"] = "Miasto" })
        };

    private async Task<JsonObject> Preview(Guid id)
        => await (await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", new { })).Content.ReadFromJsonAsync<JsonObject>();
}
