using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

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

    [Test(Description = "scenario_TEMP-015-023 - #4")]
    public async Task Given_FieldWithoutTransformations_When_UserSavesTile_Then_DetailsShowEmptyTransformationList()
    {
        var response = await Client.PostAsJsonAsync(TilesUrl, new
        {
            name = "Klient",
            fields = new[] { new { connectionId = CustomerConnectionId, path = "$.address.city", label = "Miasto" } }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var created = await response.Content.ReadFromJsonAsync<CreatedTile>();
        var tile = await GetTile(created.Id);
        Assert.That(tile["fields"]![0]!["label"]?.GetValue<string>(), Is.EqualTo("Miasto"));
        Assert.That(tile["fields"]![0]!["transformations"]!.AsArray(), Is.Empty);
    }

    [Test(Description = "scenario_TEMP-015-023 - #5")]
    public async Task Given_TileWithFieldsInGivenOrder_When_UserViewsTileDetails_Then_FieldsKeepThatOrder()
    {
        var id = await CreateTile(new
        {
            name = "Klient i kurs",
            fields = new[]
            {
                Field(RateConnectionId, "$.rate", "Kurs EUR"),
                Field(CustomerConnectionId, "$.name", "Nazwa"),
                Field(CustomerConnectionId, "$.address.city", "Miasto")
            }
        });

        var tile = await GetTile(id);

        Assert.That(
            tile["fields"]!.AsArray().Select(field => field["label"]?.GetValue<string>()),
            Is.EqualTo(new[] { "Kurs EUR", "Nazwa", "Miasto" }));
    }

    [Test(Description = "scenario_TEMP-015-023 - #18")]
    public async Task Given_TileWithRateField_When_UserViewsTileDetails_Then_NameAndFieldDefinitionAreShown()
    {
        var id = await CreateTile(new
        {
            name = "Klient i kurs",
            fields = new[] { Field(RateConnectionId, "$.rate", "Kurs EUR", Transformation("round", new { decimals = 2 })) }
        });

        var tile = await GetTile(id);

        Assert.That(
            JsonNode.DeepEquals(
                tile,
                new JsonObject
                {
                    ["id"] = id,
                    ["name"] = "Klient i kurs",
                    ["fields"] = new JsonArray(new JsonObject
                    {
                        ["connectionId"] = RateConnectionId,
                        ["path"] = "$.rate",
                        ["label"] = "Kurs EUR",
                        ["transformations"] = JsonNode.Parse("""[{"key": "round", "parameters": {"decimals": 2}}]""")
                    })
                }),
            Is.True,
            tile.ToJsonString());
    }
}
