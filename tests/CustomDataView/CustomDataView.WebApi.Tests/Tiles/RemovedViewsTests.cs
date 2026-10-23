using System.Net;
using System.Net.Http.Json;

namespace CustomDataView.WebApi.Tests.Tiles;

public class RemovedViewsTests : TileApiTests
{
    [TestCase("GET", "/api/custom-data-view/views")]
    [TestCase("POST", "/api/custom-data-view/views")]
    [TestCase("POST", "/api/custom-data-view/views/0d0c6b42-0e83-4b1a-9d0c-5a4c8f0c1a11/preview")]
    [Test(Description = "scenario_TEMP-038 - #1")]
    public async Task Given_ViewsWereReplacedByCompositeTiles_When_UserCallsViewsResource_Then_ItIsNotFound(string method, string url)
    {
        var response = method == "GET"
            ? await Client.GetAsync(url)
            : await Client.PostAsJsonAsync(url, new { name = "Pulpit", tiles = Array.Empty<object>() });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
