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
    private string levelsResponse = "[]";

    [SetUp]
    public async Task CreateDictionaryConnections()
    {
        Factory.ExternalApi.Respond = (request, _) => Task.FromResult(request.RequestUri!.AbsolutePath switch
        {
            "/statuses" => Json(StatusesResponse),
            "/orders/1" => Json(OrderResponse),
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
