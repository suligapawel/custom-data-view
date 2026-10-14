using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Views;

public class DeleteViewTests : ViewApiTests
{
    [Test(Description = "scenario_TEMP-024-028 - #24")]
    public async Task Given_Dashboard_When_UserDeletesIt_Then_ItDisappearsFromListAndDetailsAreNotFound()
    {
        var id = await CreateView("Pulpit", ViewTile(CustomerTileId, 0));

        var response = await Client.DeleteAsync($"{ViewsUrl}/{id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(await Client.GetFromJsonAsync<JsonArray>(ViewsUrl), Is.Empty);
        Assert.That((await Client.GetAsync($"{ViewsUrl}/{id}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
