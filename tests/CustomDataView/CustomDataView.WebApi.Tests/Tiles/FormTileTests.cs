using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class FormTileTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-035 - #1")]
    public async Task Given_FiltersForm_When_UserSavesIt_Then_DetailsHaveFormFieldsAndContractOutputs()
    {
        var id = await CreateTile(Filters());

        var tile = await GetTile(id);

        Assert.That(tile["display"]?.GetValue<string>(), Is.EqualTo("form"));
        Assert.That(
            JsonNode.DeepEquals(
                tile["fields"],
                JsonNode.Parse("""
                               [
                                 {"label": "Kraj", "type": "text", "required": true, "default": null},
                                 {"label": "Od", "type": "date", "required": false, "default": null},
                                 {"label": "Limit", "type": "number", "required": false, "default": 20}
                               ]
                               """)),
            Is.True,
            tile["fields"]?.ToJsonString());
        Assert.That(
            JsonNode.DeepEquals(
                tile["contract"],
                JsonNode.Parse("""{"parameters": [], "inputs": [], "outputs": {"fields": ["Kraj", "Od", "Limit"], "items": false}}""")),
            Is.True);
    }

    [TestCase("no-type", "fields[0].type")]
    [TestCase("unknown-type", "fields[0].type")]
    [TestCase("default-not-matching", "fields[2].default")]
    [TestCase("field-with-path", "fields[0]")]
    [TestCase("sources", "sources")]
    [TestCase("items", "items")]
    [TestCase("bindings", "bindings")]
    [TestCase("repeated-label", "fields[1].label")]
    [Test(Description = "scenario_TEMP-035 - #2")]
    public async Task Given_InvalidForm_When_UserSavesIt_Then_SaveIsRejectedWithFormError(string invalid, string errorKey)
    {
        var form = Filters();
        var fields = form["fields"]!.AsArray();
        switch (invalid)
        {
            case "no-type":
                fields[0]!.AsObject().Remove("type");
                break;
            case "unknown-type":
                fields[0]!["type"] = "select";
                break;
            case "default-not-matching":
                fields[2]!["default"] = "dwadzieścia";
                break;
            case "field-with-path":
                fields[0]!["path"] = "$.country";
                break;
            case "sources":
                form["sources"] = new JsonArray(new JsonObject { ["key"] = "customer", ["kind"] = "connection", ["connectionId"] = CustomerConnectionId });
                break;
            case "items":
                form["items"] = new JsonObject { ["source"] = "customer", ["path"] = "$[*]", ["key"] = "Kraj" };
                break;
            case "bindings":
                form["bindings"] = JsonNode.Parse("""[{"from": "a", "to": "b", "map": [{"field": "Kraj", "parameter": "country"}]}]""");
                break;
            case "repeated-label":
                fields[1]!["label"] = "kraj";
                break;
        }

        var response = await Client.PostAsJsonAsync(TilesUrl, form);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Does.Contain(errorKey), problem.ToJsonString());
    }

    [Test(Description = "scenario_TEMP-035 - #3")]
    public async Task Given_FiltersForm_When_UserPreviewsItWithValues_Then_FieldsHaveValuesOrDefaultsWithDescriptors()
    {
        var id = await CreateTile(Filters());

        var preview = await Preview(id, new { forms = new Dictionary<string, object> { [""] = new Dictionary<string, object> { ["Kraj"] = "PL", ["Od"] = "2026-10-01" } } });

        Assert.That(preview["display"]?.GetValue<string>(), Is.EqualTo("form"));
        Assert.That(
            JsonNode.DeepEquals(
                preview["fields"],
                JsonNode.Parse("""
                               [
                                 {"label": "Kraj", "type": "text", "required": true, "default": null, "value": "PL"},
                                 {"label": "Od", "type": "date", "required": false, "default": null, "value": "2026-10-01"},
                                 {"label": "Limit", "type": "number", "required": false, "default": 20, "value": 20}
                               ]
                               """)),
            Is.True,
            preview["fields"]?.ToJsonString());
    }

    [Test(Description = "scenario_TEMP-035 - #4")]
    public async Task Given_FiltersForm_When_UserPreviewsWithValuesNotMatchingTypes_Then_FieldsHaveInvalidValueWithGivenValue()
    {
        var id = await CreateTile(Filters());

        var preview = await Preview(id, new { forms = new Dictionary<string, object> { [""] = new Dictionary<string, object> { ["Limit"] = "abc", ["Od"] = "01.10.2026" } } });

        var fields = preview["fields"]!.AsArray();
        Assert.That(fields[1]!["error"]?["code"]?.GetValue<string>(), Is.EqualTo("invalid_value"));
        Assert.That(fields[1]!["value"]?.GetValue<string>(), Is.EqualTo("01.10.2026"));
        Assert.That(fields[2]!["error"]?["code"]?.GetValue<string>(), Is.EqualTo("invalid_value"));
        Assert.That(fields[2]!["value"]?.GetValue<string>(), Is.EqualTo("abc"));
    }

    [Test(Description = "scenario_TEMP-035 - #5")]
    public async Task Given_FiltersForm_When_UserPreviewsWithoutValues_Then_OnlyFieldWithDefaultHasValue()
    {
        var id = await CreateTile(Filters());

        var preview = await Preview(id, new { });

        Assert.That(
            preview["fields"]!.AsArray().Select(field => field!["value"]?.ToJsonString()),
            Is.EqualTo(new[] { null, null, "20" }));
    }

    private static JsonObject Filters()
        => new()
        {
            ["name"] = "Filtry",
            ["display"] = "form",
            ["fields"] = new JsonArray(
                new JsonObject { ["label"] = "Kraj", ["type"] = "text", ["required"] = true },
                new JsonObject { ["label"] = "Od", ["type"] = "date" },
                new JsonObject { ["label"] = "Limit", ["type"] = "number", ["default"] = 20 })
        };

    private async Task<JsonObject> Preview(Guid id, object values)
        => await (await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", values)).Content.ReadFromJsonAsync<JsonObject>();
}
