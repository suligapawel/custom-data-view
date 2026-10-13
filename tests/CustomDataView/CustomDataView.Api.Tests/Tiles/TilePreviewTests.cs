using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Tiles;

public class TilePreviewTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-015-023 - #28")]
    public async Task Given_SavedTileWithFieldsFromTwoConnections_When_UserPreviewsIt_Then_PreviewHasTransformedValuesInOrder()
    {
        var id = await CreateTile(new
        {
            name = "Klient i kurs",
            fields = new[]
            {
                Field(
                    CustomerConnectionId,
                    "$.temp",
                    "Temperatura",
                    Transformation("round", new { decimals = 1 }),
                    Transformation("suffix", new { text = " °C" })),
                Field(CustomerConnectionId, "$.name", "Nazwa", Transformation("uppercase")),
                Field(RateConnectionId, "$.rate", "Kurs EUR", Transformation("round", new { decimals = 2 }))
            }
        });

        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", CustomerAndRateValues());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var preview = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(preview["id"]?.GetValue<Guid>(), Is.EqualTo(id));
        Assert.That(preview["name"]?.GetValue<string>(), Is.EqualTo("Klient i kurs"));
        Assert.That(
            preview["fields"]!.AsArray().Select(field => field["label"]?.GetValue<string>()),
            Is.EqualTo(new[] { "Temperatura", "Nazwa", "Kurs EUR" }));
        AssertValues(preview, """["21.5 °C", "ACME", 4.32]""");
    }

    [TestCase(false)]
    [TestCase(true)]
    [Test(Description = "scenario_TEMP-015-023 - #14")]
    public async Task Given_SavedTileWithInvalidRoundParameter_When_UserPreviewsIt_Then_FieldHasInvalidParameterError(bool outOfRange)
    {
        var round = outOfRange ? Transformation("round", new { decimals = 20 }) : Transformation("round");
        var response = await Client.PostAsJsonAsync(TilesUrl, new
        {
            name = "Klient",
            fields = new[] { Field(CustomerConnectionId, "$.balance", "Saldo", round) }
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var id = (await response.Content.ReadFromJsonAsync<CreatedTile>()).Id;

        var preview = await Preview(id, CustomerValues());

        AssertError(preview, 0, """{"code": "transformation_failed", "transformation": "round", "reason": "invalid_parameter"}""");
    }

    [Test(Description = "scenario_TEMP-015-023 - #29")]
    public async Task Given_FieldsWithoutTransformations_When_UserPreviewsTile_Then_ValuesAreReturnedUnchanged()
    {
        var id = await CreateTile(new
        {
            name = "Klient",
            fields = new[] { Field(CustomerConnectionId, "$.balance", "Saldo"), Field(CustomerConnectionId, "$.address", "Adres") }
        });

        var preview = await Preview(id, CustomerValues());

        AssertValues(preview, """[1234.567, {"city": "Kraków"}]""");
    }

    [Test(Description = "scenario_TEMP-015-023 - #30")]
    public async Task Given_FieldWithSellersWildcardAndUppercase_When_UserPreviewsTile_Then_FieldHasTransformedList()
    {
        var id = await CreateTile(new
        {
            name = "Klient",
            fields = new[] { Field(CustomerConnectionId, "$.sellers[*].name", "Sprzedawcy", Transformation("uppercase")) }
        });

        var preview = await Preview(id, CustomerValues());

        AssertValues(preview, """[["ACME", "GLOBEX"]]""");
    }

    [Test(Description = "scenario_TEMP-015-023 - #31")]
    public async Task Given_FieldWithWildcardWithoutMatches_When_UserPreviewsTile_Then_FieldHasEmptyList()
    {
        var id = await CreateTile(new
        {
            name = "Klient",
            fields = new[] { Field(CustomerConnectionId, "$.invoices[*].number", "Faktury") }
        });

        var preview = await Preview(id, CustomerValues());

        AssertValues(preview, "[[]]");
    }

    [Test(Description = "scenario_TEMP-015-023 - #32")]
    public async Task Given_FieldWithNestedWildcardsAndSuffix_When_UserPreviewsTile_Then_FieldHasOneFlatList()
    {
        var id = await CreateTile(new
        {
            name = "Klient",
            fields = new[]
            {
                Field(CustomerConnectionId, "$.orders[*].items[*].price", "Ceny", Transformation("suffix", new { text = " zł" }))
            }
        });

        var preview = await Preview(id, CustomerValues());

        AssertValues(preview, """[["10 zł", "20 zł", "30 zł"]]""");
    }

    [TestCase("$['unit-name']", "\"Sprzedaż\"")]
    [TestCase("$.tags[0]", "\"a\"")]
    [TestCase("$.tags[-1]", "\"c\"")]
    [TestCase("$.address.*", "[\"Kraków\"]")]
    [Test(Description = "scenario_TEMP-015-023 - #33")]
    public async Task Given_FieldWithSupportedPathForm_When_UserPreviewsTile_Then_FieldHasPointedValue(string path, string value)
    {
        var id = await CreateTile(new { name = "Klient", fields = new[] { Field(CustomerConnectionId, path, "Wartość") } });

        var preview = await Preview(id, CustomerValues());

        AssertValues(preview, $"[{value}]");
    }

    [Test(Description = "scenario_TEMP-015-023 - #34")]
    public async Task Given_FieldPointingNullNoteWithUppercase_When_UserPreviewsTile_Then_FieldHasNullValueWithoutError()
    {
        var id = await CreateTile(new
        {
            name = "Klient",
            fields = new[] { Field(CustomerConnectionId, "$.note", "Notatka", Transformation("uppercase")) }
        });

        var preview = await Preview(id, CustomerValues());

        var field = preview["fields"]![0]!.AsObject();
        Assert.That(JsonNode.DeepEquals(field, JsonNode.Parse("""{"label": "Notatka", "value": null}""")), Is.True, field.ToJsonString());
    }

    private async Task<JsonObject> Preview(Guid id, object values)
    {
        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", values);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return await response.Content.ReadFromJsonAsync<JsonObject>();
    }

    private static void AssertError(JsonObject preview, int index, string expected)
    {
        var field = preview["fields"]![index]!.AsObject();
        Assert.That(JsonNode.DeepEquals(field["error"], JsonNode.Parse(expected)), Is.True, field.ToJsonString());
        Assert.That(field.ContainsKey("value"), Is.False, field.ToJsonString());
    }

    private static void AssertValues(JsonObject preview, string expected)
    {
        var values = new JsonArray(preview["fields"]!.AsArray().Select(field => field["value"]?.DeepClone()).ToArray());
        Assert.That(JsonNode.DeepEquals(values, JsonNode.Parse(expected)), Is.True, preview.ToJsonString());
    }
}
