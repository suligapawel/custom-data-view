using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Application.Dictionaries;
using Microsoft.Extensions.DependencyInjection;

namespace CustomDataView.WebApi.Tests.Tiles;

public class MapperTransformationTests : TileApiTests
{
    private const string Dir = "scenario_TEMP-045";

    private const string StatusesUrl = "https://api.example.com/statuses";

    private const string StatusesResponse =
        """
        [{"key": "NEW", "translation": "Nowe", "weight": 1}, {"key": "DONE", "translation": "Zrealizowane", "weight": 3}]
        """;

    private const string OrderResponse =
        """
        {"status": "NEW", "statuses": ["NEW", "DONE"], "level": 6, "note": null}
        """;

    private Guid statusesConnectionId;
    private Guid ordersConnectionId;
    private string levelsResponse;
    private string orderResponse;
    private bool statusesAvailable;

    [SetUp]
    public async Task CreateDictionaryConnections()
    {
        levelsResponse = "[]";
        orderResponse = OrderResponse;
        statusesAvailable = true;
        Factory.ExternalApi.Respond = (request, _) => Task.FromResult(request.RequestUri!.AbsolutePath switch
        {
            "/statuses" when statusesAvailable => Json(StatusesResponse),
            "/orders/1" => Json(orderResponse),
            "/levels" => Json(levelsResponse),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        statusesConnectionId = await CreateConnection(new { name = "Statusy", method = "GET", url = StatusesUrl });
        ordersConnectionId = await CreateConnection(new { name = "Zamówienia", method = "GET", url = "https://api.example.com/orders/1" });
    }

    [Test(Description = $"{Dir} - #2")]
    public async Task Given_FieldMappedByLoadedDictionary_When_UserPreviewsTile_Then_FieldHoldsDictionaryValueWithoutCallingDictionarySource()
    {
        var statusesId = await CreateTile(Statuses());
        var orderId = await CreateTile(Order(OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie"))));
        await LoadDictionariesOnStartup();
        var requestsBeforePreview = StatusRequests();

        var preview = await Preview(orderId);

        Assert.That(Value(preview, "Status"), Is.EqualTo("Nowe"), preview.ToJsonString());
        Assert.That(StatusRequests(), Is.EqualTo(requestsBeforePreview));
    }

    [Test(Description = $"{Dir} - #3")]
    public async Task Given_DictionaryWithNumberValue_When_UserPreviewsTile_Then_MappedValueStaysNumber()
    {
        levelsResponse = """[{"from": 6, "to": 8}]""";
        var levelsId = await CreateTile(await Levels());
        var orderId = await CreateTile(Order(
            OrderField("$.level", "Poziom", Mapper(levelsId, "Na"), Transformation("suffix", " pkt")),
            OrderField("$.level", "Poziom liczbowy", Mapper(levelsId, "Na"))));
        await LoadDictionariesOnStartup();

        var preview = await Preview(orderId);

        Assert.That(Value(preview, "Poziom"), Is.EqualTo("8 pkt"), preview.ToJsonString());
        Assert.That(JsonNode.DeepEquals(Field(preview, "Poziom liczbowy")["value"], JsonValue.Create(8)), Is.True, preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #4")]
    public async Task Given_DictionaryKeyWrittenAsText_When_UserPreviewsTileWithNumberValue_Then_NumberFindsTextKey()
    {
        levelsResponse = """[{"from": "6", "to": "wysoki"}]""";
        var levelsId = await CreateTile(await Levels());
        var orderId = await CreateTile(Order(OrderField("$.level", "Poziom", Mapper(levelsId, "Na"))));
        await LoadDictionariesOnStartup();

        var preview = await Preview(orderId);

        Assert.That(Value(preview, "Poziom"), Is.EqualTo("wysoki"), preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #5")]
    public async Task Given_DictionaryFieldsWithTransformationAndCombination_When_UserPreviewsTile_Then_DictionaryHoldsTransformedValues()
    {
        var statuses = Statuses(new JsonObject
        {
            ["label"] = "Opis",
            ["combine"] = new JsonObject { ["key"] = "template", ["parameter"] = "{Tłumaczenie} ({Waga})" }
        });
        statuses["fields"]![1]!["transformations"] = new JsonArray(new JsonObject { ["key"] = "uppercase" });
        var statusesId = await CreateTile(statuses);
        var orderId = await CreateTile(Order(
            OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie")),
            OrderField("$.status", "Opis statusu", Mapper(statusesId, "Opis"))));
        await LoadDictionariesOnStartup();

        var preview = await Preview(orderId);

        Assert.That(Value(preview, "Status"), Is.EqualTo("NOWE"), preview.ToJsonString());
        Assert.That(Value(preview, "Opis statusu"), Is.EqualTo("NOWE (1)"));
    }

    [Test(Description = $"{Dir} - #6")]
    public async Task Given_ListFieldMappedByDictionary_When_UserPreviewsTile_Then_EachElementIsMapped()
    {
        var statusesId = await CreateTile(Statuses());
        var orderId = await CreateTile(Order(OrderField("$.statuses[*]", "Historia", Mapper(statusesId, "Tłumaczenie"))));
        await LoadDictionariesOnStartup();

        var preview = await Preview(orderId);

        Assert.That(
            JsonNode.DeepEquals(Field(preview, "Historia")["value"], JsonNode.Parse("""["Nowe", "Zrealizowane"]""")),
            Is.True,
            preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #7")]
    public async Task Given_ValueMissingFromDictionary_When_UserPreviewsTile_Then_FieldHasMissingKeyErrorAndOtherFieldsHaveValues()
    {
        orderResponse = """{"status": "LOST", "level": 6}""";
        var statusesId = await CreateTile(Statuses());
        var infoId = await CreateStatusInfoTile();
        var order = Order(
            OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie")),
            OrderField("$.level", "Poziom"));
        order["children"] = new JsonArray(new JsonObject { ["key"] = "info", ["tileId"] = infoId, ["position"] = 0 });
        order["bindings"] = new JsonArray(new JsonObject
        {
            ["from"] = "parent",
            ["to"] = "info",
            ["map"] = new JsonArray(new JsonObject { ["field"] = "Status", ["parameter"] = "status" })
        });
        var orderId = await CreateTile(order);
        await LoadDictionariesOnStartup();

        var preview = await Preview(orderId);

        Assert.That(
            JsonNode.DeepEquals(
                Field(preview, "Status"),
                JsonNode.Parse("""{"label": "Status", "error": {"code": "transformation_failed", "transformation": "mapper", "reason": "missing_dictionary_key"}}""")),
            Is.True,
            preview.ToJsonString());
        var info = preview["children"]![0]!;
        Assert.That(info["state"]!.GetValue<string>(), Is.EqualTo("error"));
        Assert.That(info["error"]!["code"]!.GetValue<string>(), Is.EqualTo("dependency_failed"));
        Assert.That(info["error"]!["cause"]!["field"]!.GetValue<string>(), Is.EqualTo("Status"));
        Assert.That(Field(preview, "Poziom")["value"]!.GetValue<int>(), Is.EqualTo(6));
    }

    [Test(Description = $"{Dir} - #8")]
    public async Task Given_MissingInputValue_When_UserPreviewsTile_Then_FieldHasNeitherValueNorError()
    {
        var statusesId = await CreateTile(Statuses());
        var orderId = await CreateTile(Order(OrderField("$.note", "Notatka", Mapper(statusesId, "Tłumaczenie"))));
        await LoadDictionariesOnStartup();

        var preview = await Preview(orderId);

        Assert.That(
            JsonNode.DeepEquals(Field(preview, "Notatka"), JsonNode.Parse("""{"label": "Notatka", "value": null}""")),
            Is.True,
            preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #9")]
    public async Task Given_DictionarySourceDownOnStartup_When_TileIsSavedAgain_Then_DictionaryBecomesAvailable()
    {
        levelsResponse = """[{"from": 6, "to": 8}]""";
        var statusesId = await CreateTile(Statuses());
        var levelsId = await CreateTile(await Levels());
        var orderId = await CreateTile(Order(
            OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie")),
            OrderField("$.level", "Poziom", Mapper(levelsId, "Na"))));
        statusesAvailable = false;
        await LoadDictionariesOnStartup();

        var preview = await Preview(orderId);

        Assert.That(
            JsonNode.DeepEquals(
                Field(preview, "Status"),
                JsonNode.Parse("""{"label": "Status", "error": {"code": "transformation_failed", "transformation": "mapper", "reason": "dictionary_unavailable"}}""")),
            Is.True,
            preview.ToJsonString());
    }

    private Task LoadDictionariesOnStartup()
        => Factory.Services.GetRequiredService<DictionaryLoader>().LoadAll(CancellationToken.None);

    private int StatusRequests() => Factory.ExternalApi.Requests.Count(request => request.Url == StatusesUrl);

    private JsonObject Statuses(params JsonObject[] fields)
        => new()
        {
            ["name"] = "Statusy zamówień",
            ["display"] = "table",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "statuses", ["kind"] = "connection", ["connectionId"] = statusesConnectionId }),
            ["items"] = new JsonObject { ["source"] = "statuses", ["path"] = "$[*]", ["key"] = "Klucz" },
            ["fields"] = new JsonArray(
            [
                StatusField("$.key", "Klucz"),
                StatusField("$.translation", "Tłumaczenie"),
                StatusField("$.weight", "Waga"),
                .. fields
            ])
        };

    private async Task<Guid> CreateStatusInfoTile()
    {
        var infoConnectionId = await CreateConnection(new
        {
            name = "Opis statusu",
            method = "GET",
            url = "https://api.example.com/status-info",
            queryParameters = new[] { new { name = "status", required = true } }
        });
        return await CreateTile(new JsonObject
        {
            ["name"] = "Opis statusu",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "info", ["kind"] = "connection", ["connectionId"] = infoConnectionId }),
            ["fields"] = new JsonArray(SourceField("info", "$.description", "Opis", []))
        });
    }

    private async Task<JsonObject> Levels()
    {
        var levelsConnectionId = await CreateConnection(new { name = "Poziomy", method = "GET", url = "https://api.example.com/levels" });
        return new JsonObject
        {
            ["name"] = "Mapa poziomów",
            ["display"] = "table",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "levels", ["kind"] = "connection", ["connectionId"] = levelsConnectionId }),
            ["items"] = new JsonObject { ["source"] = "levels", ["path"] = "$[*]", ["key"] = "Z" },
            ["fields"] = new JsonArray(SourceField("levels", "$.from", "Z", []), SourceField("levels", "$.to", "Na", []))
        };
    }

    private JsonObject Order(params JsonObject[] fields)
        => new()
        {
            ["name"] = "Zamówienie",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "orders", ["kind"] = "connection", ["connectionId"] = ordersConnectionId }),
            ["fields"] = new JsonArray(fields.Select(field => (JsonNode)field).ToArray())
        };

    private static JsonObject StatusField(string path, string label, params object[] transformations)
        => SourceField("statuses", path, label, transformations);

    private static JsonObject OrderField(string path, string label, params object[] transformations)
        => SourceField("orders", path, label, transformations);

    private static JsonObject SourceField(string source, string path, string label, object[] transformations)
        => new()
        {
            ["source"] = source,
            ["path"] = path,
            ["label"] = label,
            ["transformations"] = JsonSerializer.SerializeToNode(transformations, JsonSerializerOptions.Web)
        };

    private static object Mapper(Guid dictionaryTileId, string valueLabel) => Transformation("mapper", $"{dictionaryTileId}:{valueLabel}");

    private static JsonNode Field(JsonObject preview, string label)
        => preview["fields"]!.AsArray().Single(field => field!["label"]!.GetValue<string>() == label);

    private static string Value(JsonObject preview, string label) => Field(preview, label)["value"]?.GetValue<string>();

    private async Task<JsonObject> Preview(Guid id)
    {
        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", new { });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonObject>();
    }
}
