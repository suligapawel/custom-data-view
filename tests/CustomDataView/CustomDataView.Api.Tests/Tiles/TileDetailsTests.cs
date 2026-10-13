using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Tiles;

public class TileDetailsTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-015-023 - #3")]
    public async Task Given_FieldWithRoundAndSuffix_When_UserViewsTileDetails_Then_ChainKeepsOrderAndTypedParameters()
    {
        var id = await CreateTile(new
        {
            name = "Klient",
            fields = new[]
            {
                Field(
                    CustomerConnectionId,
                    "$.balance",
                    "Saldo",
                    Transformation("round", new { decimals = 2 }),
                    Transformation("suffix", new { text = " zł" }))
            }
        });

        var tile = await GetTile(id);

        Assert.That(
            JsonNode.DeepEquals(
                tile["fields"]![0]!["transformations"],
                JsonNode.Parse("""
                               [
                                 {"key": "round", "parameters": {"decimals": 2}},
                                 {"key": "suffix", "parameters": {"text": " zł"}}
                               ]
                               """)),
            Is.True);
    }
}
