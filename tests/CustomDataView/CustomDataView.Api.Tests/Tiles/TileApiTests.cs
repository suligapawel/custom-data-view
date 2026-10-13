using System.Net.Http.Json;
using System.Text.Json.Nodes;
using CustomDataView.Api.Tests.Connections;

namespace CustomDataView.Api.Tests.Tiles;

public abstract class TileApiTests : ConnectionApiTests
{
    protected const string TilesUrl = "/api/custom-data-view/tiles";

    protected Guid CustomerConnectionId { get; private set; }
    protected Guid RateConnectionId { get; private set; }

    [SetUp]
    public async Task CreateConnections()
    {
        CustomerConnectionId = await CreateConnection(new
        {
            name = "Klient",
            method = "GET",
            url = "https://api.example.com/customers/{customer_id}",
            pathParameters = new[] { new { name = "customer_id", required = true } }
        });
        RateConnectionId = await CreateConnection(new
        {
            name = "Kurs walut",
            method = "GET",
            url = "https://api.example.com/rates",
            queryParameters = new[] { new { name = "currency", required = false } }
        });
    }

    protected async Task<Guid> CreateTile(object tile)
    {
        var response = await Client.PostAsJsonAsync(TilesUrl, tile);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedTile>();
        return created.Id;
    }

    protected Task<JsonObject> GetTile(Guid id) => Client.GetFromJsonAsync<JsonObject>($"{TilesUrl}/{id}");

    protected static object Field(Guid connectionId, string path, string label, params object[] transformations)
        => new { connectionId, path, label, transformations };

    protected static object Transformation(string key, object parameters = null) => new { key, parameters };

    protected sealed record CreatedTile(Guid Id);
}
