using System.Net;
using System.Net.Http.Json;

namespace CustomDataView.Api.Tests.Tiles;

public class MissingTileTests : TileApiTests
{
    [TestCase("6f8a9c1e-2b3d-4e5f-8a9b-0c1d2e3f4a5b")]
    [TestCase("not-a-guid")]
    [Test(Description = "scenario_TEMP-015-023 - #19")]
    public async Task Given_TileDoesNotExist_When_UserViewsTileDetails_Then_TileIsNotFound(string id)
    {
        var response = await Client.GetAsync($"{TilesUrl}/{id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test(Description = "scenario_TEMP-015-023 - #22")]
    public async Task Given_TileDoesNotExist_When_UserEditsIt_Then_TileIsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"{TilesUrl}/{Guid.NewGuid()}", new
        {
            name = "Klient",
            fields = new[] { Field(CustomerConnectionId, "$.name", "Nazwa") }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
