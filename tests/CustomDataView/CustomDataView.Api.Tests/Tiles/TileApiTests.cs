using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using CustomDataView.Api.Tests.Connections;

namespace CustomDataView.Api.Tests.Tiles;

public abstract class TileApiTests : ConnectionApiTests
{
    protected const string TilesUrl = "/api/custom-data-view/tiles";

    protected Guid CustomerConnectionId { get; private set; }
    protected Guid RateConnectionId { get; private set; }

    protected const string CustomerResponse =
        """
        {"name": "acme", "balance": 1234.567, "temp": 21.456, "note": null, "unit-name": "Sprzedaż",
         "address": {"city": "Kraków"},
         "sellers": [{"name": "acme"}, {"name": "globex"}],
         "orders": [{"items": [{"price": 10}, {"price": 20}]}, {"items": [{"price": 30}]}],
         "tags": ["a", "b", "c"]}
        """;

    protected const string CustomerUrl = "https://api.example.com/customers/42";
    protected const string RateUrl = "https://api.example.com/rates?currency=EUR";

    [SetUp]
    public async Task CreateConnections()
    {
        Factory.ExternalApi.Respond = (request, _) => Task.FromResult(request.RequestUri!.AbsoluteUri switch
        {
            CustomerUrl => Json(CustomerResponse),
            RateUrl => Json("""{"rate": 4.31789, "date": "2026-10-13"}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
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

    protected object CustomerAndRateValues()
        => new
        {
            connections = new object[]
            {
                new { connectionId = CustomerConnectionId, path = new { customer_id = "42" } },
                new { connectionId = RateConnectionId, query = new { currency = "EUR" } }
            }
        };

    protected static HttpResponseMessage Json(string body)
        => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    protected static object Field(Guid connectionId, string path, string label, params object[] transformations)
        => new { connectionId, path, label, transformations };

    protected static object Transformation(string key, object parameters = null) => new { key, parameters };

    protected sealed record CreatedTile(Guid Id);
}
