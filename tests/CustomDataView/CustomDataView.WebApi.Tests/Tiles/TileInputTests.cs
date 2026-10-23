using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class TileInputTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-034 - #1")]
    public async Task Given_OrderLinesTile_When_UserPreviewsItWithRowsInput_Then_ItemsComeFromInputWithoutCalls()
    {
        var id = await CreateTile(OrderLines());

        var preview = await Preview(id, new { inputs = new { rows = new[] { new { sku = "A-1", qty = 2 }, new { sku = "B-7", qty = 1 } } } });

        Assert.That(
            JsonNode.DeepEquals(
                preview["items"],
                JsonNode.Parse("""
                               [
                                 {"key": "A-1", "fields": [{"label": "SKU", "value": "A-1"}, {"label": "Ilość", "value": 2}]},
                                 {"key": "B-7", "fields": [{"label": "SKU", "value": "B-7"}, {"label": "Ilość", "value": 1}]}
                               ]
                               """)),
            Is.True,
            preview.ToJsonString());
        Assert.That(Factory.ExternalApi.Requests, Is.Empty);
    }

    [Test(Description = "scenario_TEMP-034 - #2")]
    public async Task Given_OrderLinesTile_When_UserPreviewsItWithoutRowsInput_Then_ItemsErrorPointsAtMissingInput()
    {
        var id = await CreateTile(OrderLines());

        var preview = await Preview(id, new { });

        Assert.That(preview["itemsError"]?["code"]?.GetValue<string>(), Is.EqualTo("invalid_parameters"));
        Assert.That(preview["itemsError"]!["parameters"]![0]!["name"]?.GetValue<string>(), Is.EqualTo("rows"));
    }

    [Test(Description = "scenario_TEMP-034 - #3")]
    public async Task Given_OrderLinesTile_When_UserViewsDetails_Then_ContractHasRowsInputAndItemOutputs()
    {
        var id = await CreateTile(OrderLines());

        var tile = await GetTile(id);

        Assert.That(
            JsonNode.DeepEquals(
                tile["contract"],
                JsonNode.Parse("""{"parameters": [], "inputs": ["rows"], "outputs": {"fields": ["SKU", "Ilość"], "items": true}}""")),
            Is.True,
            tile["contract"]?.ToJsonString());
    }

    [TestCase("input-with-connection", "sources[0].connectionId")]
    [TestCase("unknown-kind", "sources[0].kind")]
    [Test(Description = "scenario_TEMP-034 - #4")]
    public async Task Given_InvalidInputSource_When_UserSavesTile_Then_SaveIsRejectedWithSourceError(string invalid, string errorKey)
    {
        var tile = OrderLines();
        if (invalid == "input-with-connection")
        {
            tile["sources"]![0]!["connectionId"] = CustomerConnectionId;
        }
        else
        {
            tile["sources"]![0]!["kind"] = "file";
        }

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Does.Contain(errorKey));
    }

    [Test(Description = "scenario_TEMP-034 - #5")]
    public async Task Given_TileMixingConnectionAndInput_When_UserPreviewsWithBoth_Then_FieldsComeFromBothSources()
    {
        var tile = Tile("Klient z notatką", Field(CustomerConnectionId, "$.name", "Nazwa"));
        tile["sources"]!.AsArray().Add(new JsonObject { ["key"] = "note", ["kind"] = "input" });
        tile["fields"]!.AsArray().Add(new JsonObject { ["source"] = "note", ["path"] = "$.text", ["label"] = "Notatka" });
        var id = await CreateTile(tile);

        var preview = await Preview(id, new { parameters = new { customer_id = "42" }, inputs = new { note = new { text = "VIP" } } });

        Assert.That(
            JsonNode.DeepEquals(preview["fields"], JsonNode.Parse("""[{"label": "Nazwa", "value": "acme"}, {"label": "Notatka", "value": "VIP"}]""")),
            Is.True,
            preview.ToJsonString());
    }

    private static JsonObject OrderLines()
        => new()
        {
            ["name"] = "Pozycje zamówienia",
            ["display"] = "table",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "rows", ["kind"] = "input" }),
            ["items"] = new JsonObject { ["source"] = "rows", ["path"] = "$[*]", ["key"] = "SKU" },
            ["fields"] = new JsonArray(
                new JsonObject { ["source"] = "rows", ["path"] = "$.sku", ["label"] = "SKU" },
                new JsonObject { ["source"] = "rows", ["path"] = "$.qty", ["label"] = "Ilość" })
        };

    private async Task<JsonObject> Preview(Guid id, object values)
        => await (await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", values)).Content.ReadFromJsonAsync<JsonObject>();
}
