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
                new { name = "pageNumber", required = false },
                new { name = "pageSize", required = false },
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

    [Test(Description = $"{Dir} - #10")]
    public async Task Given_NextPageValues_When_FrontPreviewsNextPage_Then_SecondPageWithPreviousIsReturned()
    {
        var id = await CreateTile(Cities());
        var next = (await Preview(id, new { parameters = new { name = "Kra" } }))["pagination"]!["next"]!.DeepClone();

        var preview = await Preview(id, next);

        Assert.That(Factory.ExternalApi.Requests[^1].Url, Is.EqualTo("https://api.example.com/cities?name=Kra&pageNumber=2&pageSize=10"));
        Assert.That(preview["items"]!.AsArray(), Has.Count.EqualTo(2));
        Assert.That(
            JsonNode.DeepEquals(
                preview["pagination"],
                JsonNode.Parse("""{"page": 2, "pageSize": 10, "total": 12, "pages": 2, "previous": {"parameters": {"name": "Kra", "pageNumber": 1, "pageSize": 10}}}""")),
            Is.True,
            preview["pagination"]?.ToJsonString());
    }

    [TestCase(10, true)]
    [TestCase(4, false)]
    [Test(Description = $"{Dir} - #11")]
    public async Task Given_PaginationWithoutTotal_When_UserPreviewsFirstPage_Then_NextDependsOnFullPage(int count, bool hasNext)
    {
        pageCount = count;
        var tile = Cities();
        tile["pagination"]!.AsObject().Remove("totalPath");
        var id = await CreateTile(tile);

        var pagination = (await Preview(id, new { }))["pagination"]!;

        Assert.That(pagination["next"] is not null, Is.EqualTo(hasNext), pagination.ToJsonString());
        Assert.That(pagination["pages"], Is.Null);
        Assert.That(pagination["total"], Is.Null);
    }

    [Test(Description = $"{Dir} - #12")]
    public async Task Given_CustomPageParameters_When_FrontPreviewsWithPageSize_Then_ApiGetsPageAndLimit()
    {
        var tile = Cities();
        tile["pagination"] = new JsonObject { ["pageNumberParameter"] = "page", ["pageSizeParameter"] = "limit", ["pageSize"] = 10 };
        var id = await CreateTile(tile);

        var preview = await Preview(id, new { parameters = new { limit = 5 } });

        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url), Is.EqualTo(new[] { "https://api.example.com/cities?page=1&limit=5" }));
        Assert.That(preview["pagination"]!["pageSize"]?.GetValue<int>(), Is.EqualTo(5));
    }

    [TestCase("missing", "pagination")]
    [TestCase("page-size-zero", "pagination.pageSize")]
    [TestCase("page-size-101", "pagination.pageSize")]
    [TestCase("undeclared-page-number", "pagination.pageNumberParameter")]
    [TestCase("same-names", "pagination.pageSizeParameter")]
    [TestCase("total-with-wildcard", "pagination.totalPath")]
    [TestCase("input-source", "pagination")]
    [Test(Description = $"{Dir} - #13")]
    public async Task Given_InvalidPagination_When_UserSavesTile_Then_SaveIsRejected(string invalid, string errorKey)
    {
        var tile = Cities();
        var pagination = tile["pagination"]!.AsObject();
        switch (invalid)
        {
            case "missing":
                tile.Remove("pagination");
                break;
            case "page-size-zero":
                pagination["pageSize"] = 0;
                break;
            case "page-size-101":
                pagination["pageSize"] = 101;
                break;
            case "undeclared-page-number":
                pagination["pageNumberParameter"] = "offset";
                break;
            case "same-names":
                pagination["pageNumberParameter"] = "page";
                pagination["pageSizeParameter"] = "page";
                break;
            case "total-with-wildcard":
                pagination["totalPath"] = "$.cities[*].id";
                break;
            case "input-source":
                tile["sources"] = new JsonArray(new JsonObject { ["key"] = "cities", ["kind"] = "input" });
                break;
        }

        await AssertRejected(tile, errorKey);
    }

    [Test(Description = $"{Dir} - #14")]
    public async Task Given_TableWithPagination_When_UserSavesTile_Then_SaveIsRejected()
    {
        var tile = Cities();
        tile["display"] = "table";

        await AssertRejected(tile, "pagination");
    }

    private async Task AssertRejected(JsonObject tile, string errorKey)
    {
        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Does.Contain(errorKey), problem.ToJsonString());
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
