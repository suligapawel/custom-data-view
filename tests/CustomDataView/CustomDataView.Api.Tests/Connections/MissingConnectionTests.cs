using System.Net;

namespace CustomDataView.Api.Tests.Connections;

public class MissingConnectionTests : ConnectionApiTests
{
    [Test(Description = "scenario_TEMP-007-014 - #23")]
    public async Task Given_UnknownConnectionId_When_UserBrowsesDetails_Then_ConnectionIsNotFound()
    {
        var response = await Client.GetAsync($"{ConnectionsUrl}/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
