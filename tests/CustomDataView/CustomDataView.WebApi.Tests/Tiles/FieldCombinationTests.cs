using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class FieldCombinationTests : TileApiTests
{
    private const string Dir = "scenario_TEMP-044";

    private const string PlaceResponse =
        """
        {"street": "Długa", "number": "5", "city": "Kraków", "lat": 50.0647, "lon": 19.9461,
         "email": null, "phone": "123 456 789", "tags": ["zabytki", null, "rynek"]}
        """;

    private Guid placeConnectionId;

    [SetUp]
    public async Task CreatePlaceConnection()
    {
        Factory.ExternalApi.Respond = (request, _) => Task.FromResult(request.RequestUri!.AbsolutePath switch
        {
            "/places/1" => Json(PlaceResponse),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        placeConnectionId = await CreateConnection(new { name = "Miejsce", method = "GET", url = "https://api.example.com/places/1" });
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
