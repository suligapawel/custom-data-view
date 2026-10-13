using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Tiles;

public class AddTileTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-015-023 - #1")]
    public async Task Given_NoTile_When_UserSavesTileWithFieldsFromTwoConnections_Then_TileIsSavedAndIdReturned()
    {
        var response = await Client.PostAsJsonAsync(TilesUrl, new
        {
            name = "Klient i kurs",
            fields = new[]
            {
                Field(CustomerConnectionId, "$.name", "Nazwa", Transformation("uppercase")),
                Field(RateConnectionId, "$.rate", "Kurs EUR", Transformation("round", new { decimals = 2 }))
            }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }
}
