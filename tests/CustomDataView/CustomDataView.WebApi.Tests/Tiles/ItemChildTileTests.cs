using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class ItemChildTileTests : TileApiTests
{
    private const string OrdersUrl = "https://api.example.com/orders";
    private const string DefaultOrders = """{"orders": [{"id": 17, "lines": [{"sku": "A-1", "qty": 2}]}, {"id": 18, "lines": []}]}""";

    private string ordersBody;
    private Guid ordersConnectionId;
    private Guid shippingConnectionId;
    private Guid linesTileId;
    private Guid shippingTileId;

    [SetUp]
    public async Task CreateTiles()
    {
        ordersBody = DefaultOrders;
        Factory.ExternalApi.Respond = (request, _) =>
        {
            var url = request.RequestUri!.AbsoluteUri;
            return Task.FromResult(url switch
            {
                OrdersUrl => Json(ordersBody),
                _ when url.StartsWith("https://api.example.com/shipping?order=", StringComparison.Ordinal)
                    => Json($$"""{"status": "sent-{{url.Split('=')[1]}}"}"""),
                _ when url.StartsWith("https://api.example.com/stock?", StringComparison.Ordinal) => Json("""{"total": 5}"""),
                _ when url.StartsWith("https://api.example.com/extra", StringComparison.Ordinal) => Json("""{"value": 1}"""),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            });
        };
        ordersConnectionId = await CreateConnection(new { name = "Zamówienia", method = "GET", url = OrdersUrl });
        shippingConnectionId = await CreateConnection(new
        {
            name = "Dostawa",
            method = "GET",
            url = "https://api.example.com/shipping",
            queryParameters = new[] { new { name = "order", required = true } }
        });
        linesTileId = await CreateTile(new JsonObject
        {
            ["name"] = "Pozycje zamówienia",
            ["display"] = "table",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "rows", ["kind"] = "input" }),
            ["items"] = new JsonObject { ["source"] = "rows", ["path"] = "$[*]", ["key"] = "SKU" },
            ["fields"] = new JsonArray(Data("rows", "$.sku", "SKU"), Data("rows", "$.qty", "Ilość"))
        });
        shippingTileId = await CreateTile(DataTile("Dostawa", "shipping", shippingConnectionId, Data("shipping", "$.status", "Status")));
    }

    [Test(Description = "scenario_TEMP-037 - #1")]
    public async Task Given_OrdersTableWithItemChildren_When_UserPreviewsIt_Then_EachItemHasOwnChildInstances()
    {
        var id = await CreateTile(Orders());

        var preview = await Preview(id);

        var first = preview["items"]![0]!["children"]!.AsArray();
        Assert.That(first.Select(child => child!["address"]!.GetValue<string>()), Is.EqualTo(new[] { "[17]/lines", "[17]/shipping" }));
        Assert.That(first[0]!["tile"]!["items"]!.AsArray().Select(item => item!["key"]!.GetValue<string>()), Is.EqualTo(new[] { "A-1" }));
        Assert.That(first[1]!["tile"]!["fields"]![0]!["value"]?.GetValue<string>(), Is.EqualTo("sent-17"));
        var second = preview["items"]![1]!["children"]!.AsArray();
        Assert.That(second[0]!["tile"]!["items"]!.AsArray(), Is.Empty);
        Assert.That(second[1]!["tile"]!["fields"]![0]!["value"]?.GetValue<string>(), Is.EqualTo("sent-18"));
    }

    [Test(Description = "scenario_TEMP-037 - #2")]
    public async Task Given_LazyShippingInItems_When_UserPreviewsOrders_Then_ShippingAwaitsLoadWithItemInputs()
    {
        var orders = Orders();
        orders["children"]![1]!["load"] = "lazy";
        var id = await CreateTile(orders);

        var preview = await Preview(id);

        var shipping = preview["items"]![0]!["children"]![1]!;
        Assert.That(shipping["state"]?.GetValue<string>(), Is.EqualTo("awaiting_load"));
        Assert.That(JsonNode.DeepEquals(shipping["inputs"], JsonNode.Parse("""{"parameters": {"order": 17}}""")), Is.True, shipping.ToJsonString());
        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url), Is.EqualTo(new[] { OrdersUrl }));
    }

    [TestCase("item-to-tile-child", "bindings[1].map[0].scope")]
    [TestCase("item-from-child", "bindings[2].map[0].scope")]
    [TestCase("item-child-to-tile-child", "bindings[2].to")]
    [Test(Description = "scenario_TEMP-037 - #3")]
    public async Task Given_InvalidItemScope_When_UserSavesTile_Then_SaveIsRejectedWithBindingError(string invalid, string errorKey)
    {
        var orders = Orders();
        orders["children"]!.AsArray().Add(Child("summary", shippingTileId, 2, "tile"));
        var bindings = orders["bindings"]!.AsArray();
        switch (invalid)
        {
            case "item-to-tile-child":
                orders["children"]![1]!["scope"] = "tile";
                break;
            case "item-from-child":
                bindings.Add(Binding("lines", "shipping", ("SKU", "order", "item")));
                break;
            case "item-child-to-tile-child":
                bindings.Add(Binding("shipping", "summary", ("Status", "order", null)));
                break;
        }

        var response = await Client.PostAsJsonAsync(TilesUrl, orders);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Does.Contain(errorKey), problem.ToJsonString());
    }

    [Test(Description = "scenario_TEMP-037 - #4")]
    public async Task Given_FiftyOneOrders_When_UserPreviewsOrders_Then_ShippingBeyondFiftiethItemAwaitsLoad()
    {
        ordersBody = OrdersBody(51);
        var id = await CreateTile(Orders());

        var preview = await Preview(id);

        var states = preview["items"]!.AsArray().Select(item => item!["children"]![1]!["state"]!.GetValue<string>()).ToList();
        Assert.That(states.Take(50), Is.All.EqualTo("data"));
        Assert.That(states[50], Is.EqualTo("awaiting_load"));
        Assert.That(preview["items"]![50]!["children"]![1]!["inputs"]!["parameters"]!["order"]?.GetValue<int>(), Is.EqualTo(51));
    }

    [Test(Description = "scenario_TEMP-037 - #5")]
    public async Task Given_FiveItemChildrenOnFiftyOrders_When_UserPreviewsOrders_Then_OnlyTwoHundredCallsAreMade()
    {
        ordersBody = OrdersBody(50);
        var orders = Orders();
        var children = new JsonArray();
        var bindings = new JsonArray();
        for (var index = 0; index < 5; index++)
        {
            var connectionId = await CreateConnection(new
            {
                name = $"Extra {index}",
                method = "GET",
                url = $"https://api.example.com/extra{index}",
                queryParameters = new[] { new { name = "order", required = true } }
            });
            var tileId = await CreateTile(DataTile($"Extra {index}", "extra", connectionId, Data("extra", "$.value", "Wartość")));
            children.Add(Child($"extra{index}", tileId, index, "item"));
            bindings.Add(Binding("parent", $"extra{index}", ("ID", "order", "item")));
        }

        orders["children"] = children;
        orders["bindings"] = bindings;
        var id = await CreateTile(orders);

        var preview = await Preview(id);

        var states = preview["items"]!.AsArray().SelectMany(item => item!["children"]!.AsArray().Select(child => child!["state"]!.GetValue<string>())).ToList();
        Assert.That(Factory.ExternalApi.Requests, Has.Count.EqualTo(200));
        Assert.That(states.Count(state => state == "awaiting_load"), Is.EqualTo(51));
        Assert.That(states.Count(state => state == "data"), Is.EqualTo(199));
    }

    [Test(Description = "scenario_TEMP-037 - #6")]
    public async Task Given_FirstChildCallTakesOverTenSeconds_When_UserPreviews_Then_DependentChildAwaitsLoad()
    {
        Factory.ExternalApi.Respond = (request, _) =>
        {
            Factory.Time.Advance(TimeSpan.FromSeconds(11));
            return Task.FromResult(Json("""{"status": "late"}"""));
        };
        var tile = new JsonObject
        {
            ["name"] = "Dostawy",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "note", ["kind"] = "input" }),
            ["fields"] = new JsonArray(Data("note", "$.text", "Notatka"))
        };
        tile["children"] = new JsonArray(Child("first", shippingTileId, 0, "tile"), Child("second", shippingTileId, 1, "tile"));
        tile["bindings"] = new JsonArray(Binding("first", "second", ("Status", "order", null)));
        var id = await CreateTile(tile);

        var preview = await Preview(id, new
        {
            inputs = new { note = new { text = "pilne" } },
            descendants = new Dictionary<string, object> { ["first"] = new { parameters = new { order = "1" } } }
        });

        Assert.That(preview["children"]![0]!["state"]?.GetValue<string>(), Is.EqualTo("data"));
        Assert.That(preview["children"]![1]!["state"]?.GetValue<string>(), Is.EqualTo("awaiting_load"));
    }

    [Test(Description = "scenario_TEMP-037 - #7")]
    public async Task Given_TwentyItemInstancesAndRepeatedEmbedding_When_UserPreviews_Then_ParallelismIsLimitedAndCallsAreDeduplicated()
    {
        ordersBody = OrdersBody(20);
        var running = 0;
        var maxRunning = 0;
        var respond = Factory.ExternalApi.Respond;
        Factory.ExternalApi.Respond = async (request, token) =>
        {
            var now = Interlocked.Increment(ref running);
            InterlockedMax(ref maxRunning, now);
            await Task.Delay(20, token);
            Interlocked.Decrement(ref running);
            return await respond(request, token);
        };
        var orders = Orders();
        orders["children"]!.AsArray().Add(Child("again", shippingTileId, 2, "item"));
        orders["bindings"]!.AsArray().Add(Binding("parent", "again", ("ID", "order", "item")));
        var id = await CreateTile(orders);

        await Preview(id);

        Assert.That(maxRunning, Is.InRange(2, 8));
        Assert.That(Factory.ExternalApi.Requests.Count(request => request.Url.Contains("/shipping?order=1&") || request.Url.EndsWith("/shipping?order=1")), Is.EqualTo(1));
        Assert.That(Factory.ExternalApi.Requests, Has.Count.EqualTo(21));
    }

    [Test(Description = "scenario_TEMP-037 - #8")]
    public async Task Given_SiblingBindingInsideItem_When_UserPreviewsOrders_Then_StockGetsSkusOfSameItem()
    {
        var stockConnectionId = await CreateConnection(new
        {
            name = "Stan",
            method = "GET",
            url = "https://api.example.com/stock",
            queryParameters = new[] { new { name = "sku", required = true } }
        });
        var stockTileId = await CreateTile(DataTile("Stan", "stock", stockConnectionId, Data("stock", "$.total", "Razem")));
        var orders = Orders();
        orders["children"]!.AsArray().Add(Child("stock", stockTileId, 2, "item"));
        orders["bindings"]!.AsArray().Add(Binding("lines", "stock", ("SKU", "sku", "all")));
        var id = await CreateTile(orders);

        var preview = await Preview(id);

        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url), Does.Contain("https://api.example.com/stock?sku=A-1"));
        Assert.That(preview["items"]![0]!["children"]![2]!["tile"]!["fields"]![0]!["value"]?.GetValue<int>(), Is.EqualTo(5));
        Assert.That(
            preview["items"]![1]!["children"]![2]!["tile"]!["fields"]![0]!["error"]!["code"]?.GetValue<string>(),
            Is.EqualTo("invalid_parameters"));
    }

    private JsonObject Orders()
    {
        var orders = new JsonObject
        {
            ["name"] = "Zamówienia",
            ["display"] = "table",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "orders", ["kind"] = "connection", ["connectionId"] = ordersConnectionId }),
            ["items"] = new JsonObject { ["source"] = "orders", ["path"] = "$.orders[*]", ["key"] = "ID" },
            ["fields"] = new JsonArray(Data("orders", "$.id", "ID"), Data("orders", "$.lines", "Linie", hidden: true)),
            ["children"] = new JsonArray(Child("lines", linesTileId, 0, "item"), Child("shipping", shippingTileId, 1, "item")),
            ["bindings"] = new JsonArray(
                InputBinding("parent", "lines", "Linie", "rows"),
                Binding("parent", "shipping", ("ID", "order", "item")))
        };
        return orders;
    }

    private static string OrdersBody(int count)
        => new JsonObject
        {
            ["orders"] = new JsonArray(Enumerable.Range(1, count).Select(id => (JsonNode)new JsonObject { ["id"] = id, ["lines"] = new JsonArray() }).ToArray())
        }.ToJsonString();

    private static void InterlockedMax(ref int target, int value)
    {
        int current;
        while ((current = Volatile.Read(ref target)) < value && Interlocked.CompareExchange(ref target, value, current) != current)
        {
        }
    }

    private static JsonObject DataTile(string name, string key, Guid connectionId, params JsonObject[] fields)
        => new()
        {
            ["name"] = name,
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = key, ["kind"] = "connection", ["connectionId"] = connectionId }),
            ["fields"] = new JsonArray(fields.Cast<JsonNode>().ToArray())
        };

    private static JsonObject Data(string source, string path, string label, bool hidden = false)
        => new() { ["source"] = source, ["path"] = path, ["label"] = label, ["hidden"] = hidden };

    private static JsonObject Child(string key, Guid tileId, int position, string scope)
        => new() { ["key"] = key, ["tileId"] = tileId, ["position"] = position, ["scope"] = scope, ["load"] = "eager" };

    private static JsonObject Binding(string from, string to, params (string Field, string Parameter, string Scope)[] map)
        => new()
        {
            ["from"] = from,
            ["to"] = to,
            ["map"] = new JsonArray(map
                .Select(mapping => (JsonNode)new JsonObject { ["field"] = mapping.Field, ["parameter"] = mapping.Parameter, ["scope"] = mapping.Scope })
                .ToArray())
        };

    private static JsonObject InputBinding(string from, string to, string field, string input)
        => new()
        {
            ["from"] = from,
            ["to"] = to,
            ["map"] = new JsonArray(new JsonObject { ["field"] = field, ["input"] = input, ["scope"] = "item" })
        };

    private async Task<JsonObject> Preview(Guid id, object values = null)
        => await (await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", values ?? new { })).Content.ReadFromJsonAsync<JsonObject>();
}
