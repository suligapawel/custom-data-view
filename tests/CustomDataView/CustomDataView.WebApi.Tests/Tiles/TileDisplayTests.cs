using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class TileDisplayTests : TileApiTests
{
    private const string CitiesUrl = "https://api.example.com/cities";

    private Guid citiesConnectionId;

    [SetUp]
    public async Task CreateCitiesConnection()
    {
        var respond = Factory.ExternalApi.Respond;
        Factory.ExternalApi.Respond = (request, cancellationToken) => request.RequestUri!.AbsoluteUri == CitiesUrl
            ? Task.FromResult(Json("""{"cities": [{"id": 1, "name": "Kraków", "temp": 21.456}, {"id": 2, "name": "Gdańsk", "temp": 17.04}]}"""))
            : respond(request, cancellationToken);
        citiesConnectionId = await CreateConnection(new
        {
            name = "Miasta",
            method = "GET",
            url = "https://api.example.com/cities",
            queryParameters = new[] { new { name = "country", required = false } }
        });
    }

    [TestCase(null)]
    [TestCase("chart")]
    [Test(Description = "scenario_TEMP-032 - #1")]
    public async Task Given_TileWithoutOrWithUnknownDisplay_When_UserSavesIt_Then_SaveIsRejectedWithDisplayError(string display)
    {
        var tile = Tile("Klient", Field(CustomerConnectionId, "$.name", "Nazwa"));
        tile["display"] = display;

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        await AssertRejected(response, "display");
    }

    [Test(Description = "scenario_TEMP-032 - #2")]
    public async Task Given_DetailsTile_When_UserViewsDetailsAndPreview_Then_BothHaveDetailsDisplayAndPreviewHasFields()
    {
        var id = await CreateTile(Tile("Klient", Field(CustomerConnectionId, "$.name", "Nazwa")));

        var tile = await GetTile(id);
        var preview = await Preview(id, CustomerValues());

        Assert.That(tile["display"]?.GetValue<string>(), Is.EqualTo("details"));
        Assert.That(preview["display"]?.GetValue<string>(), Is.EqualTo("details"));
        Assert.That(JsonNode.DeepEquals(preview["fields"], JsonNode.Parse("""[{"label": "Nazwa", "value": "acme"}]""")), Is.True);
    }

    [Test(Description = "scenario_TEMP-032 - #3")]
    public async Task Given_CitiesTable_When_UserPreviewsIt_Then_ItemsHaveKeysAndFieldsRelativeToItem()
    {
        var id = await CreateTile(CitiesTable());

        var preview = await Preview(id, new { });

        Assert.That(preview["display"]?.GetValue<string>(), Is.EqualTo("table"));
        Assert.That(preview["itemKey"]?.GetValue<string>(), Is.EqualTo("ID"));
        Assert.That(preview.ContainsKey("fields"), Is.False);
        Assert.That(
            JsonNode.DeepEquals(
                preview["items"],
                JsonNode.Parse("""
                               [
                                 {"key": 1, "fields": [{"label": "ID", "value": 1}, {"label": "Miasto", "value": "Kraków"}, {"label": "Temperatura", "value": 21.5}]},
                                 {"key": 2, "fields": [{"label": "ID", "value": 2}, {"label": "Miasto", "value": "Gdańsk"}, {"label": "Temperatura", "value": 17}]}
                               ]
                               """)),
            Is.True,
            preview["items"]?.ToJsonString());
    }

    [TestCase("list", null, "fields")]
    [TestCase("details", "title", "fields[0].role")]
    [TestCase("list", "primary", "fields[0].role")]
    [Test(Description = "scenario_TEMP-032 - #4")]
    public async Task Given_TileWithRoleNotMatchingDisplay_When_UserSavesIt_Then_SaveIsRejectedWithRoleError(string display, string role, string errorKey)
    {
        var tile = display == "list" ? CitiesTable() : Tile("Klient", Field(CustomerConnectionId, "$.name", "Nazwa"));
        tile["display"] = display;
        tile["fields"]![0]!["role"] = role;
        if (display == "list" && role is not null)
        {
            tile["fields"]![1]!["role"] = "title";
        }

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        await AssertRejected(response, errorKey);
    }

    [Test(Description = "scenario_TEMP-032 - #5")]
    public async Task Given_SummaryTile_When_UserSavesItWithoutAndWithPrimaryRole_Then_OnlyTileWithPrimaryIsSavedWithRoles()
    {
        var tile = Tile("Klient", Field(CustomerConnectionId, "$.balance", "Saldo"), Field(CustomerConnectionId, "$.name", "Nazwa"));
        tile["display"] = "summary";
        tile["fields"]![1]!["role"] = "secondary";

        var rejected = await Client.PostAsJsonAsync(TilesUrl, tile);
        tile["fields"]![0]!["role"] = "primary";
        var id = await CreateTile(tile);

        await AssertRejected(rejected, "fields");
        var saved = await GetTile(id);
        Assert.That(saved["fields"]!.AsArray().Select(field => field["role"]?.GetValue<string>()), Is.EqualTo(new[] { "primary", "secondary" }));
    }

    [TestCase("table-without-items", "items")]
    [TestCase("details-with-items", "items")]
    [TestCase("unknown-source", "items.source")]
    [TestCase("no-wildcard", "items.path")]
    [TestCase("unknown-key", "items.key")]
    [TestCase("visible-other-source", "fields[3].source")]
    [Test(Description = "scenario_TEMP-032 - #6")]
    public async Task Given_TileWithInvalidItems_When_UserSavesIt_Then_SaveIsRejectedWithItemsOrFieldError(string invalid, string errorKey)
    {
        var tile = CitiesTable();
        switch (invalid)
        {
            case "table-without-items":
                tile.Remove("items");
                break;
            case "details-with-items":
                tile["display"] = "details";
                break;
            case "unknown-source":
                tile["items"]!["source"] = "orders";
                break;
            case "no-wildcard":
                tile["items"]!["path"] = "$.cities[0]";
                break;
            case "unknown-key":
                tile["items"]!["key"] = "Kod";
                break;
            case "visible-other-source":
                AddCustomerField(tile, hidden: false);
                break;
        }

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        await AssertRejected(response, errorKey);
    }

    [Test(Description = "scenario_TEMP-032 - #7")]
    public async Task Given_TableWithHiddenFieldFromOtherSource_When_UserSavesIt_Then_TileIsSaved()
    {
        var tile = CitiesTable();
        AddCustomerField(tile, hidden: true);

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    private JsonObject CitiesTable()
        => new()
        {
            ["name"] = "Miasta",
            ["display"] = "table",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "cities", ["kind"] = "connection", ["connectionId"] = citiesConnectionId }),
            ["items"] = new JsonObject { ["source"] = "cities", ["path"] = "$.cities[*]", ["key"] = "ID" },
            ["fields"] = new JsonArray(
                new JsonObject { ["source"] = "cities", ["path"] = "$.id", ["label"] = "ID" },
                new JsonObject { ["source"] = "cities", ["path"] = "$.name", ["label"] = "Miasto" },
                new JsonObject
                {
                    ["source"] = "cities",
                    ["path"] = "$.temp",
                    ["label"] = "Temperatura",
                    ["transformations"] = JsonNode.Parse("""[{"key": "round", "parameters": {"decimals": 1}}]""")
                })
        };

    private void AddCustomerField(JsonObject tile, bool hidden)
    {
        tile["sources"]!.AsArray().Add(new JsonObject { ["key"] = "customer", ["kind"] = "connection", ["connectionId"] = CustomerConnectionId });
        tile["fields"]!.AsArray().Add(new JsonObject { ["source"] = "customer", ["path"] = "$.name", ["label"] = "Klient", ["hidden"] = hidden });
    }

    private async Task<JsonObject> Preview(Guid id, object values)
        => await (await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", values)).Content.ReadFromJsonAsync<JsonObject>();

    private static async Task AssertRejected(HttpResponseMessage response, string errorKey)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Does.Contain(errorKey), problem.ToJsonString());
    }
}
