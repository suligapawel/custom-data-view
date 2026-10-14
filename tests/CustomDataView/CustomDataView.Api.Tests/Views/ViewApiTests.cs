using System.Net.Http.Json;
using System.Text.Json.Nodes;
using CustomDataView.Api.Tests.Tiles;

namespace CustomDataView.Api.Tests.Views;

public abstract class ViewApiTests : TileApiTests
{
    protected const string ViewsUrl = "/api/custom-data-view/views";

    protected Guid CustomerTileId { get; private set; }
    protected Guid RateTileId { get; private set; }
    protected Guid OrdersTileId { get; private set; }

    [SetUp]
    public async Task CreateTiles()
    {
        CustomerTileId = await CreateTile(new { name = "Klient", fields = new[] { Field(CustomerConnectionId, "$.name", "Nazwa") } });
        RateTileId = await CreateTile(new { name = "Kurs walut", fields = new[] { Field(RateConnectionId, "$.rate", "Kurs") } });
        OrdersTileId = await CreateTile(new { name = "Zamówienia", fields = new[] { Field(CustomerConnectionId, "$.orders", "Zamówienia") } });
    }

    protected async Task<Guid> CreateView(string name, params object[] tiles)
    {
        var response = await Client.PostAsJsonAsync(ViewsUrl, new { name, tiles });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedView>();
        return created.Id;
    }

    protected Task<JsonObject> GetView(Guid id) => Client.GetFromJsonAsync<JsonObject>($"{ViewsUrl}/{id}");

    protected static IEnumerable<(Guid?, string, int?, string)> Tiles(JsonObject view)
        => view["tiles"]!.AsArray().Select(tile => (
            tile["tileId"]?.GetValue<Guid>(),
            tile["tileName"]?.GetValue<string>(),
            tile["position"]?.GetValue<int>(),
            tile["cssClass"]?.GetValue<string>()));

    protected static object ViewTile(Guid tileId, int position, string cssClass = null) => new { tileId, position, cssClass };

    protected sealed record CreatedView(Guid Id);
}
