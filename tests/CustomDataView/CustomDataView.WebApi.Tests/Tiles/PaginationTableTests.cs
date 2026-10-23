using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace CustomDataView.WebApi.Tests.Tiles;

public class PaginationTableTests : TileApiTests
{
    private const string Dir = "scenario_TEMP-041-043";

    private Guid citiesConnectionId;
    private int pageCount = 10;

    [SetUp]
    public async Task CreateConnection()
    {
        pageCount = 10;
        Factory.ExternalApi.Respond = (request, _) =>
        {
            var query = HttpUtility.ParseQueryString(request.RequestUri!.Query);
            var page = int.Parse(query["pageNumber"] ?? query["page"] ?? "1");
            var count = page == 1 ? pageCount : 2;
            var cities = new JsonArray(Enumerable.Range(1, count)
                .Select(index => (JsonNode)new JsonObject { ["id"] = (page - 1) * 10 + index, ["name"] = $"Miasto {(page - 1) * 10 + index}" })
                .ToArray());
            return Task.FromResult(Json(new JsonObject { ["total"] = 12, ["cities"] = cities }.ToJsonString()));
        };
        citiesConnectionId = await CreateConnection(new
        {
            name = "Miasta",
            method = "GET",
            url = "https://api.example.com/cities",
            queryParameters = new object[]
            {
                new { name = "name", required = false },
                new { name = "pageNumber", required = true },
                new { name = "pageSize", required = true },
                new { name = "page", required = false },
                new { name = "limit", required = false }
            }
        });
    }

    [Test(Description = $"{Dir} - #9")]
    public async Task Given_PaginationTable_When_UserPreviewsFirstPage_Then_FirstPageWithTotalAndNextIsReturned()
    {
        var id = await CreateTile(Cities());

        var preview = await Preview(id, new { parameters = new { name = "Kra" } });

        Assert.That(
            Factory.ExternalApi.Requests.Select(request => request.Url),
            Is.EqualTo(new[] { "https://api.example.com/cities?name=Kra&pageNumber=1&pageSize=10" }));
        Assert.That(preview["items"]!.AsArray(), Has.Count.EqualTo(10));
        Assert.That(
            JsonNode.DeepEquals(
                preview["pagination"],
                JsonNode.Parse("""{"page": 1, "pageSize": 10, "total": 12, "pages": 2, "next": {"parameters": {"name": "Kra", "pageNumber": 2, "pageSize": 10}}}""")),
            Is.True,
            preview["pagination"]?.ToJsonString());
    }

    private JsonObject Cities()
        => new()
        {
            ["name"] = "Wyniki",
            ["display"] = "pagination_table",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "cities", ["kind"] = "connection", ["connectionId"] = citiesConnectionId }),
            ["items"] = new JsonObject { ["source"] = "cities", ["path"] = "$.cities[*]", ["key"] = "ID" },
            ["pagination"] = new JsonObject { ["pageSize"] = 10, ["totalPath"] = "$.total" },
            ["fields"] = new JsonArray(
                new JsonObject { ["source"] = "cities", ["path"] = "$.id", ["label"] = "ID" },
                new JsonObject { ["source"] = "cities", ["path"] = "$.name", ["label"] = "Miasto" })
        };

    private async Task<JsonObject> Preview(Guid id, object values)
    {
        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", values);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonObject>();
    }
}
