using System.Net.Http.Json;
using System.Text.Json.Nodes;
using CustomDataView.WebApi.Tests.Connections;

namespace CustomDataView.WebApi.Tests.Combinations;

public class CombinationCatalogTests : ConnectionApiTests
{
    private const string Dir = "scenario_TEMP-044";

    [Test(Description = $"{Dir} - #1")]
    public async Task Given_ConfiguredModule_When_UserReadsCombinationCatalog_Then_EachCombinationDescribesParameterAndFields()
    {
        var catalog = await Client.GetFromJsonAsync<JsonArray>("/api/custom-data-view/combinations");

        Assert.That(
            catalog!.Select(combination => combination!["key"]!.GetValue<string>()),
            Is.EqualTo(new[] { "coalesce", "join", "object", "template" }));
        Assert.That(catalog.Select(combination => combination!["name"]!.GetValue<string>()), Has.All.Not.Empty);
        Assert.That(catalog.Select(combination => combination!["description"]!.GetValue<string>()), Has.All.Not.Empty);
        var combinations = catalog.ToDictionary(combination => combination!["key"]!.GetValue<string>());
        Assert.That(
            JsonNode.DeepEquals(
                combinations["join"],
                JsonNode.Parse(
                    """
                    {"key": "join", "name": "Złączenie", "description": "Łączy wartości pól w kolejności listy separatorem; pomija brak wartości i rozkłada listy.",
                     "parameter": {"name": "separator", "type": "string", "required": false}, "minFields": 1, "maxFields": null}
                    """)),
            Is.True,
            combinations["join"]!.ToJsonString());
        Assert.That(
            JsonNode.DeepEquals(combinations["template"]!["parameter"], JsonNode.Parse("""{"name": "pattern", "type": "string", "required": true}""")),
            Is.True);
        Assert.That(combinations["template"]!["minFields"]!.GetValue<int>(), Is.EqualTo(0));
        Assert.That(combinations["template"]!["maxFields"]!.GetValue<int>(), Is.EqualTo(0));
        Assert.That(combinations["object"]!["parameter"], Is.Null);
        Assert.That(combinations["object"]!["minFields"]!.GetValue<int>(), Is.EqualTo(1));
        Assert.That(combinations["coalesce"]!["parameter"], Is.Null);
        Assert.That(combinations["coalesce"]!["minFields"]!.GetValue<int>(), Is.EqualTo(2));
    }
}
