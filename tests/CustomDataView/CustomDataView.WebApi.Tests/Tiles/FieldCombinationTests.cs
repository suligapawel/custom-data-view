using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace CustomDataView.WebApi.Tests.Tiles;

public class FieldCombinationTests : TileApiTests
{
    private const string Dir = "scenario_TEMP-044";

    private const string CitiesResponse =
        """
        {"cities": [{"id": 1, "name": "Kraków", "country": "PL"}, {"id": 2, "name": "Praga", "country": "CZ"}]}
        """;

    private const string PlaceResponse =
        """
        {"street": "Długa", "number": "5", "city": "Kraków", "lat": 50.0647, "lon": 19.9461,
         "email": null, "phone": "123 456 789", "tags": ["zabytki", null, "rynek"]}
        """;

    private Guid placeConnectionId;
    private Guid citiesConnectionId;

    [SetUp]
    public async Task CreatePlaceConnection()
    {
        Factory.ExternalApi.Respond = (request, _) => Task.FromResult(request.RequestUri!.AbsolutePath switch
        {
            "/places/1" => Json(PlaceResponse),
            "/cities" => Json(CitiesResponse),
            "/weather" => Json("""{"temp": 12.5}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        placeConnectionId = await CreateConnection(new { name = "Miejsce", method = "GET", url = "https://api.example.com/places/1" });
        citiesConnectionId = await CreateConnection(new { name = "Miasta", method = "GET", url = "https://api.example.com/cities" });
    }

    [Test(Description = $"{Dir} - #2")]
    public async Task Given_TemplateFromHiddenFields_When_UserPreviewsTile_Then_FieldHoldsComposedAndTransformedText()
    {
        var tile = Address(
            Combined("Pełny adres", "template", "{Ulica} {Nr}, {Miasto}"),
            Combined("Adres wielkimi", "template", "{ulica} {Nr}, {Miasto}", transformations: [Transformation("uppercase")]));
        var id = await CreateTile(tile);

        var preview = await Preview(id);

        Assert.That(Value(preview, "Pełny adres"), Is.EqualTo("Długa 5, Kraków"), preview.ToJsonString());
        Assert.That(Value(preview, "Adres wielkimi"), Is.EqualTo("DŁUGA 5, KRAKÓW"));
        Assert.That(
            new[] { "Ulica", "Nr", "Miasto" }.Select(label => Field(preview, label)["hidden"]?.GetValue<bool>()),
            Has.All.True);
    }

    [Test(Description = $"{Dir} - #3")]
    public async Task Given_TemplateWithMissingValueAndDoubledBraces_When_UserPreviewsTile_Then_MissingValueIsEmptyAndBracesAreLiteral()
    {
        RespondWithPlace(place => place["number"] = null);
        var id = await CreateTile(Address(Combined("Etykieta", "template", "{{{Ulica} {Nr}}}")));

        var preview = await Preview(id);

        Assert.That(Value(preview, "Etykieta"), Is.EqualTo("{Długa }"), preview.ToJsonString());
    }

    [TestCase("Ulica|Nr|Miasto", "Długa, 5, Kraków")]
    [TestCase("Miasto|Tagi", "Kraków, zabytki, rynek")]
    [TestCase("Lat|Lon", "50.06, 19.95")]
    [Test(Description = $"{Dir} - #4")]
    public async Task Given_JoinOfFields_When_UserPreviewsTile_Then_ValuesAreJoinedSkippingMissingAndSpreadingLists(string fields, string expected)
    {
        var id = await CreateTile(Address(PlaceField("$.tags[*]", "Tagi"), Combined("Złączone", "join", ", ", fields.Split('|'))));

        var preview = await Preview(id);

        Assert.That(Value(preview, "Złączone"), Is.EqualTo(expected), preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #5")]
    public async Task Given_JoinOfMissingValuesAndOfObject_When_UserPreviewsTile_Then_FirstHasNoValueAndSecondHasCombinationError()
    {
        var id = await CreateTile(Address(
            PlaceField("$.email", "Email"),
            Combined("Bez wartości", "join", fields: ["Email"]),
            PlaceField("$", "Całość"),
            Combined("Błędne", "join", fields: ["Miasto", "Całość"])));

        var preview = await Preview(id);

        Assert.That(
            JsonNode.DeepEquals(Field(preview, "Bez wartości"), JsonNode.Parse("""{"label": "Bez wartości", "value": null}""")),
            Is.True,
            preview.ToJsonString());
        Assert.That(
            JsonNode.DeepEquals(
                Field(preview, "Błędne"),
                JsonNode.Parse("""{"label": "Błędne", "error": {"code": "combination_failed", "combination": "join", "reason": "unsupported_value"}}""")),
            Is.True,
            preview.ToJsonString());
        Assert.That(Value(preview, "Miasto"), Is.EqualTo("Kraków"));
    }

    [Test(Description = $"{Dir} - #6")]
    public async Task Given_ObjectBoundToChildDataInput_When_UserPreviewsTile_Then_ChildShowsMembersOfObject()
    {
        var mapId = await CreateTile(new JsonObject
        {
            ["name"] = "Mapa",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "point", ["kind"] = "input" }),
            ["fields"] = new JsonArray(
                new JsonObject { ["source"] = "point", ["path"] = "$.Lat", ["label"] = "Szerokość" },
                new JsonObject { ["source"] = "point", ["path"] = "$.Lon", ["label"] = "Długość" })
        });
        var tile = Address(Combined("Punkt", "object", fields: ["Lat", "Lon"], hidden: true));
        tile["children"] = new JsonArray(new JsonObject { ["key"] = "map", ["tileId"] = mapId, ["position"] = 0 });
        tile["bindings"] = new JsonArray(new JsonObject
        {
            ["from"] = "parent",
            ["to"] = "map",
            ["map"] = new JsonArray(new JsonObject { ["field"] = "Punkt", ["input"] = "point" })
        });
        var id = await CreateTile(tile);
        Factory.ExternalApi.Respond = async (_, cancellationToken) =>
        {
            await Task.Delay(50, cancellationToken);
            return Json(PlaceResponse);
        };

        var preview = await Preview(id);

        Assert.That(
            JsonNode.DeepEquals(Field(preview, "Punkt")["value"], JsonNode.Parse("""{"Lat": 50.06, "Lon": 19.95}""")),
            Is.True,
            preview.ToJsonString());
        var map = preview["children"]![0]!["tile"]!;
        Assert.That(
            map["fields"]!.AsArray().Select(field => (field!["label"]!.GetValue<string>(), field["value"]?.GetValue<decimal>())),
            Is.EqualTo(new (string, decimal?)[] { ("Szerokość", 50.06m), ("Długość", 19.95m) }),
            preview.ToJsonString());
    }

    [TestCase("Email|Telefon", "123 456 789")]
    [TestCase("Telefon|Email", "123 456 789")]
    [TestCase("Email|Email2", null)]
    [Test(Description = $"{Dir} - #7")]
    public async Task Given_FirstAvailableValueOfFields_When_UserPreviewsTile_Then_FirstValueOtherThanMissingIsReturned(string fields, string expected)
    {
        var id = await CreateTile(Address(
            PlaceField("$.email", "Email"),
            PlaceField("$.phone", "Telefon"),
            PlaceField("$.email", "Email2"),
            Combined("Kontakt", "coalesce", fields: fields.Split('|'))));

        var preview = await Preview(id);

        Assert.That(Field(preview, "Kontakt")["error"], Is.Null, preview.ToJsonString());
        Assert.That(Value(preview, "Kontakt"), Is.EqualTo(expected));
    }

    [Test(Description = $"{Dir} - #8")]
    public async Task Given_ListWithCombinedTitleAndItemAction_When_UserPreviewsList_Then_EachItemHasItsTitleAndActionCarriesIt()
    {
        var cityConnectionId = await CreateConnection(new
        {
            name = "Miasto po tytule",
            method = "GET",
            url = "https://api.example.com/city",
            queryParameters = new[] { new { name = "title", required = true } }
        });
        var cityTileId = await CreateTile(new JsonObject
        {
            ["name"] = "Miasto",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "city", ["kind"] = "connection", ["connectionId"] = cityConnectionId }),
            ["fields"] = new JsonArray(new JsonObject { ["source"] = "city", ["path"] = "$.name", ["label"] = "Nazwa" })
        });
        var list = Cities("list", Combined("Tytuł", "template", "{Nazwa} ({Kraj})"));
        list["fields"]!.AsArray().Last()!["role"] = "title";
        list["actions"] = new JsonArray(new JsonObject
        {
            ["key"] = "details",
            ["label"] = "Szczegóły",
            ["tileId"] = cityTileId,
            ["map"] = new JsonArray(new JsonObject { ["field"] = "Tytuł", ["parameter"] = "title" })
        });
        var id = await CreateTile(list);

        var preview = await Preview(id);

        var items = preview["items"]!.AsArray();
        Assert.That(
            items.Select(item => item!["fields"]!.AsArray().Single(field => field!["label"]!.GetValue<string>() == "Tytuł")!["value"]?.GetValue<string>()),
            Is.EqualTo(new[] { "Kraków (PL)", "Praga (CZ)" }),
            preview.ToJsonString());
        Assert.That(items[1]!["actions"]![0]!["inputs"]!["parameters"]!["title"]?.GetValue<string>(), Is.EqualTo("Praga (CZ)"));
    }

    [Test(Description = $"{Dir} - #9")]
    public async Task Given_TemplateBoundToChildConnectionParameter_When_UserPreviewsTile_Then_ChildConnectionIsCalledWithComposedValue()
    {
        var weatherTileId = await CreateWeatherTile();
        var tile = Address(Combined("Współrzędne", "template", "{Lat},{Lon}"));
        tile["children"] = new JsonArray(new JsonObject { ["key"] = "weather", ["tileId"] = weatherTileId, ["position"] = 0 });
        tile["bindings"] = new JsonArray(new JsonObject
        {
            ["from"] = "parent",
            ["to"] = "weather",
            ["map"] = new JsonArray(new JsonObject { ["field"] = "Współrzędne", ["parameter"] = "latlon" })
        });
        var id = await CreateTile(tile);

        var preview = await Preview(id);

        Assert.That(LatLonOfWeatherRequest(), Is.EqualTo("50.06,19.95"), preview.ToJsonString());
        Assert.That(preview["children"]![0]!["tile"]!["fields"]![0]!["value"]?.GetValue<decimal>(), Is.EqualTo(12.5m));
    }

    private void RespondWithPlace(Action<JsonObject> change)
    {
        var place = JsonNode.Parse(PlaceResponse)!.AsObject();
        change(place);
        Factory.ExternalApi.Respond = (_, _) => Task.FromResult(Json(place.ToJsonString()));
    }

    private async Task<Guid> CreateWeatherTile()
    {
        var weatherConnectionId = await CreateConnection(new
        {
            name = "Pogoda",
            method = "GET",
            url = "https://api.example.com/weather",
            queryParameters = new[] { new { name = "latlon", required = true } }
        });
        return await CreateTile(new JsonObject
        {
            ["name"] = "Pogoda",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "weather", ["kind"] = "connection", ["connectionId"] = weatherConnectionId }),
            ["fields"] = new JsonArray(new JsonObject { ["source"] = "weather", ["path"] = "$.temp", ["label"] = "Temperatura" })
        });
    }

    private string LatLonOfWeatherRequest()
        => HttpUtility.ParseQueryString(new Uri(Factory.ExternalApi.Requests.Single(request => request.Url.Contains("/weather")).Url).Query)["latlon"];

    private JsonObject Address(params JsonObject[] fields)
        => new()
        {
            ["name"] = "Adres",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "api", ["kind"] = "connection", ["connectionId"] = placeConnectionId }),
            ["fields"] = new JsonArray(
            [
                PlaceField("$.street", "Ulica"),
                PlaceField("$.number", "Nr"),
                PlaceField("$.city", "Miasto"),
                PlaceField("$.lat", "Lat", Transformation("round", 2)),
                PlaceField("$.lon", "Lon", Transformation("round", 2)),
                .. fields
            ])
        };

    private JsonObject Cities(string display, params JsonObject[] fields)
        => new()
        {
            ["name"] = "Miasta",
            ["display"] = display,
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "cities", ["kind"] = "connection", ["connectionId"] = citiesConnectionId }),
            ["items"] = new JsonObject { ["source"] = "cities", ["path"] = "$.cities[*]", ["key"] = "ID" },
            ["fields"] = new JsonArray(
            [
                new JsonObject { ["source"] = "cities", ["path"] = "$.id", ["label"] = "ID" },
                new JsonObject { ["source"] = "cities", ["path"] = "$.name", ["label"] = "Nazwa", ["hidden"] = true },
                new JsonObject { ["source"] = "cities", ["path"] = "$.country", ["label"] = "Kraj", ["hidden"] = true },
                .. fields
            ])
        };

    private static JsonObject PlaceField(string path, string label, params object[] transformations)
        => new()
        {
            ["source"] = "api",
            ["path"] = path,
            ["label"] = label,
            ["hidden"] = true,
            ["transformations"] = JsonSerializer.SerializeToNode(transformations, JsonSerializerOptions.Web)
        };

    private static JsonObject Combined(
        string label,
        string key,
        string parameter = null,
        string[] fields = null,
        object[] transformations = null,
        bool hidden = false)
        => new()
        {
            ["label"] = label,
            ["combine"] = new JsonObject
            {
                ["key"] = key,
                ["parameter"] = parameter,
                ["fields"] = fields is null ? null : new JsonArray(fields.Select(field => (JsonNode)field).ToArray())
            },
            ["hidden"] = hidden,
            ["transformations"] = JsonSerializer.SerializeToNode(transformations ?? [], JsonSerializerOptions.Web)
        };

    private static JsonNode Field(JsonObject preview, string label)
        => preview["fields"]!.AsArray().Single(field => field!["label"]!.GetValue<string>() == label);

    private static string Value(JsonObject preview, string label) => Field(preview, label)["value"]?.GetValue<string>();

    private async Task<JsonObject> Preview(Guid id, object values = null)
    {
        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", values ?? new { });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonObject>();
    }
}
