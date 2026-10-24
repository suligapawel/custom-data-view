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

    [SetUp]
    public async Task CreateDictionaryConnections()
    {
        Factory.ExternalApi.Respond = (request, _) => Task.FromResult(request.RequestUri!.AbsolutePath switch
        {
            "/statuses" => Json(StatusesResponse),
            "/orders/1" => Json(OrderResponse),
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
