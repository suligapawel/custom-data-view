using System.Net.Http.Json;
using System.Text.Json.Nodes;
using CustomDataView.WebApi.Tests.Connections;

namespace CustomDataView.WebApi.Tests.Transformations;

public class MapperCatalogTests : ConnectionApiTests
{
    private const string Dir = "scenario_TEMP-045";

    [Test(Description = $"{Dir} - #1")]
    public async Task Given_ConfiguredModule_When_UserReadsTransformationCatalog_Then_MapperDescribesDictionaryParameter()
    {
        var catalog = await Client.GetFromJsonAsync<JsonArray>("/api/custom-data-view/transformations");

        var mapper = catalog!.SingleOrDefault(transformation => transformation!["key"]!.GetValue<string>() == "mapper");
        Assert.That(
            JsonNode.DeepEquals(
                mapper,
                JsonNode.Parse(
                    """
                    {"key": "mapper", "name": "Słownik", "description": "Zamienia wartość na wartość ze słownika z kafelka.",
                     "parameter": {"name": "dictionary", "type": "string"}}
                    """)),
            Is.True,
            catalog.ToJsonString());
    }
}
