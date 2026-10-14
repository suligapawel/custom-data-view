using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Views;

public class ViewValidationTests : ViewApiTests
{
    [TestCase(null)]
    [TestCase("   ")]
    [TestCase(101)]
    [Test(Description = "scenario_TEMP-024-028 - #9")]
    public async Task Given_ViewWithInvalidName_When_UserSavesIt_Then_SaveIsRejectedWithNameError(object name)
    {
        var view = ValidView();
        view["name"] = name is int length ? new string('a', length) : (string)name;

        var response = await Client.PostAsJsonAsync(ViewsUrl, view);

        await AssertRejected(response, "name");
    }

    private JsonObject ValidView()
        => new()
        {
            ["name"] = "Pulpit",
            ["tiles"] = new JsonArray(
                new JsonObject { ["tileId"] = CustomerTileId, ["position"] = 0 },
                new JsonObject { ["tileId"] = RateTileId, ["position"] = 1 })
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
