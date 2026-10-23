using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class TileValidationTests : TileApiTests
{
    [TestCase(null)]
    [TestCase("   ")]
    [TestCase(101)]
    [Test(Description = "scenario_TEMP-015-023 - #7")]
    public async Task Given_TileWithInvalidName_When_UserSavesIt_Then_SaveIsRejectedWithNameError(object name)
    {
        var tile = ValidTile();
        tile["name"] = name is int length ? new string('a', length) : (string)name;

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        await AssertRejected(response, "name");
    }

    [TestCase(true)]
    [TestCase(false)]
    [Test(Description = "scenario_TEMP-015-023 - #8")]
    public async Task Given_TileWithoutFields_When_UserSavesIt_Then_SaveIsRejectedWithFieldsError(bool emptyList)
    {
        var tile = new JsonObject { ["name"] = "Pusty" };
        if (emptyList)
        {
            tile["fields"] = new JsonArray();
        }

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        await AssertRejected(response, "fields");
    }

    [TestCase(null)]
    [TestCase("   ")]
    [TestCase(101)]
    [Test(Description = "scenario_TEMP-015-023 - #9")]
    public async Task Given_SecondFieldWithInvalidLabel_When_UserSavesTile_Then_SaveIsRejectedWithSecondLabelError(object label)
    {
        var tile = ValidTile();
        tile["fields"]![1]!["label"] = label is int length ? new string('a', length) : (string)label;

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        await AssertRejected(response, "fields[1].label");
    }

    [TestCase(null)]
    [TestCase("sellers[0]")]
    [TestCase("$..name")]
    [TestCase("$.items[?(@.x)]")]
    [TestCase("$.items[0:2]")]
    [TestCase("$.a[")]
    [Test(Description = "scenario_TEMP-015-023 - #10")]
    public async Task Given_FirstFieldWithInvalidPath_When_UserSavesTile_Then_SaveIsRejectedWithFirstPathError(string path)
    {
        var tile = ValidTile();
        tile["fields"]![0]!["path"] = path;

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        await AssertRejected(response, "fields[0].path");
    }

    [Test(Description = "scenario_TEMP-015-023 - #11")]
    public async Task Given_FirstFieldWithoutConnection_When_UserSavesTile_Then_SaveIsRejectedWithFirstConnectionError()
    {
        var tile = ValidTile();
        tile["fields"]![0]!.AsObject().Remove("connectionId");

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        await AssertRejected(response, "fields[0].connectionId");
    }

    [Test(Description = "scenario_TEMP-015-023 - #11")]
    public async Task Given_FirstFieldWithMissingConnection_When_UserSavesTile_Then_SaveIsRejectedWithConnectionIdInError()
    {
        var missingId = Guid.NewGuid();
        var tile = ValidTile();
        tile["fields"]![0]!["connectionId"] = missingId;

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        var errors = await AssertRejected(response, "fields[0].connectionId");
        Assert.That(errors["fields[0].connectionId"]![0]!.GetValue<string>(), Does.Contain(missingId.ToString()));
    }

    [Test(Description = "scenario_TEMP-015-023 - #12")]
    public async Task Given_FirstFieldWithReverseTransformation_When_UserSavesTile_Then_SaveIsRejectedWithKeyInError()
    {
        var tile = ValidTile();
        tile["fields"]![0]!["transformations"] = new JsonArray(new JsonObject { ["key"] = "reverse" });

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        var errors = await AssertRejected(response, "fields[0].transformations[0].key");
        Assert.That(errors["fields[0].transformations[0].key"]![0]!.GetValue<string>(), Does.Contain("reverse"));
    }

    [Test(Description = "scenario_TEMP-015-023 - #13")]
    public async Task Given_SuffixWithObjectTextParameter_When_UserSavesTile_Then_SaveIsRejectedWithParameterError()
    {
        var tile = ValidTile();
        tile["fields"]![0]!["transformations"] = new JsonArray(new JsonObject
        {
            ["key"] = "suffix",
            ["parameters"] = new JsonObject { ["text"] = new JsonObject { ["value"] = " zł" } }
        });

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        await AssertRejected(response, "fields[0].transformations[0].parameters.text");
    }

    [Test(Description = "scenario_TEMP-015-023 - #15")]
    public async Task Given_CustomerTile_When_UserSavesTileWithEmptyName_Then_SaveIsRejectedAndListIsUnchanged()
    {
        var id = await CreateTile(ValidTile());
        var tile = ValidTile();
        tile["name"] = string.Empty;

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var tiles = await Client.GetFromJsonAsync<JsonArray>(TilesUrl);
        Assert.That(
            tiles.Select(saved => (saved["id"]?.GetValue<Guid>(), saved["name"]?.GetValue<string>())),
            Is.EqualTo(new (Guid?, string)[] { (id, "Klient") }));
    }

    private JsonObject ValidTile()
        => new()
        {
            ["name"] = "Klient",
            ["fields"] = new JsonArray(
                new JsonObject { ["connectionId"] = CustomerConnectionId, ["path"] = "$.name", ["label"] = "Nazwa" },
                new JsonObject { ["connectionId"] = CustomerConnectionId, ["path"] = "$.balance", ["label"] = "Saldo" })
        };

    private static async Task<JsonObject> AssertRejected(HttpResponseMessage response, string errorKey)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        var errors = problem["errors"]!.AsObject();
        Assert.That(errors.Select(error => error.Key), Does.Contain(errorKey));
        return errors;
    }
}
