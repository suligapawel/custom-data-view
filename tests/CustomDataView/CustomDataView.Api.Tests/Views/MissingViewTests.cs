using System.Net;

namespace CustomDataView.Api.Tests.Views;

public class MissingViewTests : ViewApiTests
{
    [TestCase("6f8a9c1e-2b3d-4e5f-8a9b-0c1d2e3f4a5b")]
    [TestCase("not-a-guid")]
    [Test(Description = "scenario_TEMP-024-028 - #20")]
    public async Task Given_ViewDoesNotExist_When_UserViewsDetails_Then_ViewIsNotFound(string id)
    {
        var response = await Client.GetAsync($"{ViewsUrl}/{id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
