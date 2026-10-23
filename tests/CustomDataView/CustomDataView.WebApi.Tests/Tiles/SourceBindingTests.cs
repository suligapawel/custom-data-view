using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class SourceBindingTests : TileApiTests
{
    private const string ClientUrl = "https://api.example.com/clients/42";

    private Guid clientConnectionId;
    private Guid ordersConnectionId;

    [SetUp]
    public async Task CreateClientAndOrdersConnections()
    {
        Factory.ExternalApi.Respond = (request, _) => Task.FromResult(request.RequestUri!.AbsoluteUri switch
        {
            ClientUrl => Json("""{"id": 7, "name": "acme", "region": "eu", "score": null}"""),
            "https://api.example.com/orders?customer=7&region=eu" => Json("""{"count": 3}"""),
            "https://api.example.com/orders?customer=7&region=all" => Json("""{"count": 9}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        clientConnectionId = await CreateConnection(new
        {
            name = "Klient",
            method = "GET",
            url = "https://api.example.com/clients/{customer_id}",
            pathParameters = new[] { new { name = "customer_id", required = true } }
        });
        ordersConnectionId = await CreateConnection(OrdersConnection(withRegion: true));
    }

    [Test(Description = "scenario_TEMP-033 - #4")]
    public async Task Given_BindingWithTwoMappings_When_UserPreviewsTile_Then_OrdersAreCalledAfterClientWithBothValues()
    {
        var id = await CreateTile(CustomerOrders());

        var preview = await Preview(id, ClientValues());

        Assert.That(
            Factory.ExternalApi.Requests.Select(request => request.Url),
            Is.EqualTo(new[] { ClientUrl, "https://api.example.com/orders?customer=7&region=eu" }));
        Assert.That(Field(preview, "Liczba")["value"]?.GetValue<int>(), Is.EqualTo(3));
    }

    [TestCase("unknown-node", "bindings[0].to")]
    [TestCase("field-from-other-source", "bindings[0].map[0].field")]
    [TestCase("undeclared-parameter", "bindings[0].map[0].parameter")]
    [TestCase("same-node", "bindings[0].to")]
    [TestCase("repeated-pair", "bindings[1]")]
    [TestCase("empty-map", "bindings[0].map")]
    [TestCase("parameter-fed-twice", "bindings[0].map[1].parameter")]
    [TestCase("cycle", "bindings")]
    [Test(Description = "scenario_TEMP-033 - #5")]
    public async Task Given_TileWithInvalidBinding_When_UserSavesIt_Then_SaveIsRejectedWithBindingError(string invalid, string errorKey)
    {
        var tile = CustomerOrders();
        var bindings = tile["bindings"]!.AsArray();
        var map = bindings[0]!["map"]!.AsArray();
        switch (invalid)
        {
            case "unknown-node":
                bindings[0]!["to"] = "invoices";
                break;
            case "field-from-other-source":
                map[0]!["field"] = "Liczba";
                break;
            case "undeclared-parameter":
                map[0]!["parameter"] = "status";
                break;
            case "same-node":
                bindings[0]!["to"] = "client";
                break;
            case "repeated-pair":
                bindings.Add(bindings[0]!.DeepClone());
                break;
            case "empty-map":
                map.Clear();
                break;
            case "parameter-fed-twice":
                map[1]!["parameter"] = "customer";
                break;
            case "cycle":
                bindings.Add(Binding("orders", "client", ("Liczba", "customer_id")));
                break;
        }

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Does.Contain(errorKey), problem.ToJsonString());
    }

    [Test(Description = "scenario_TEMP-033 - #6")]
    public async Task Given_ClientSourceUnavailable_When_UserPreviewsTile_Then_OrdersFieldHasDependencyFailedWithoutCall()
    {
        var id = await CreateTile(CustomerOrders());
        Factory.ExternalApi.Respond = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var preview = await Preview(id, ClientValues());

        Assert.That(Field(preview, "ID klienta")["error"]?["code"]?.GetValue<string>(), Is.EqualTo("source_unavailable"));
        Assert.That(
            JsonNode.DeepEquals(
                Field(preview, "Liczba")["error"],
                JsonNode.Parse("""{"code": "dependency_failed", "cause": {"node": "client", "field": "ID klienta", "code": "source_unavailable"}}""")),
            Is.True,
            Field(preview, "Liczba").ToJsonString());
        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url), Is.EqualTo(new[] { ClientUrl }));
    }

    [Test(Description = "scenario_TEMP-033 - #7")]
    public async Task Given_OrdersConnectionNoLongerDeclaresRegion_When_UserPreviewsTile_Then_OrdersFieldHasInvalidBindingWithoutCall()
    {
        var id = await CreateTile(CustomerOrders());
        (await Client.PutAsJsonAsync($"{ConnectionsUrl}/{ordersConnectionId}", OrdersConnection(withRegion: false))).EnsureSuccessStatusCode();

        var preview = await Preview(id, ClientValues());

        var error = Field(preview, "Liczba")["error"]!;
        Assert.That(error["code"]?.GetValue<string>(), Is.EqualTo("invalid_binding"));
        Assert.That(error["parameters"]![0]!["name"]?.GetValue<string>(), Is.EqualTo("region"));
        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url), Is.EqualTo(new[] { ClientUrl }));
    }

    [Test(Description = "scenario_TEMP-033 - #8")]
    public async Task Given_RegionBoundToNullField_When_UserPreviewsTile_Then_OrdersAreCalledWithDefaultRegion()
    {
        var tile = CustomerOrders();
        tile["fields"]!.AsArray().Add(new JsonObject { ["source"] = "client", ["path"] = "$.score", ["label"] = "Wynik", ["hidden"] = true });
        tile["bindings"]![0]!["map"]![1]!["field"] = "Wynik";
        var id = await CreateTile(tile);

        await Preview(id, ClientValues());

        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url).Last(), Is.EqualTo("https://api.example.com/orders?customer=7&region=all"));
    }

    [Test(Description = "scenario_TEMP-033 - #9")]
    public async Task Given_TileWithSourceBinding_When_UserViewsDetails_Then_ContractHasOnlyUnboundParametersAndAllOutputs()
    {
        var id = await CreateTile(CustomerOrders());

        var tile = await GetTile(id);

        Assert.That(
            JsonNode.DeepEquals(
                tile["contract"],
                JsonNode.Parse("""
                               {"parameters": [{"name": "customer_id", "required": true}], "inputs": [],
                                "outputs": {"fields": ["ID klienta", "Region", "Liczba"], "items": false}}
                               """)),
            Is.True,
            tile["contract"]?.ToJsonString());
        Assert.That(JsonNode.DeepEquals(tile["bindings"], CustomerOrders()["bindings"]), Is.True, tile["bindings"]?.ToJsonString());
    }

    [Test(Description = "scenario_TEMP-033 - #10")]
    public async Task Given_RegionBoundFromClient_When_UserPreviewsWithTestRegion_Then_BoundRegionWins()
    {
        var id = await CreateTile(CustomerOrders());

        await Preview(id, new { parameters = new { customer_id = "42", region = "us" } });

        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url).Last(), Is.EqualTo("https://api.example.com/orders?customer=7&region=eu"));
    }

    private JsonObject CustomerOrders()
        => new()
        {
            ["name"] = "Zamówienia klienta",
            ["display"] = "details",
            ["sources"] = new JsonArray(
                new JsonObject { ["key"] = "client", ["kind"] = "connection", ["connectionId"] = clientConnectionId },
                new JsonObject { ["key"] = "orders", ["kind"] = "connection", ["connectionId"] = ordersConnectionId }),
            ["fields"] = new JsonArray(
                new JsonObject { ["source"] = "client", ["path"] = "$.id", ["label"] = "ID klienta", ["hidden"] = true },
                new JsonObject { ["source"] = "client", ["path"] = "$.region", ["label"] = "Region", ["hidden"] = true },
                new JsonObject { ["source"] = "orders", ["path"] = "$.count", ["label"] = "Liczba" }),
            ["bindings"] = new JsonArray(Binding("client", "orders", ("ID klienta", "customer"), ("Region", "region")))
        };

    private static JsonObject Binding(string from, string to, params (string Field, string Parameter)[] map)
        => new()
        {
            ["from"] = from,
            ["to"] = to,
            ["map"] = new JsonArray(map.Select(mapping => (JsonNode)new JsonObject
            {
                ["field"] = mapping.Field,
                ["parameter"] = mapping.Parameter
            }).ToArray())
        };

    private static object OrdersConnection(bool withRegion)
        => new
        {
            name = "Zamówienia",
            method = "GET",
            url = "https://api.example.com/orders",
            queryParameters = withRegion
                ? new object[] { new { name = "customer", required = true }, new { name = "region", required = false, defaultValue = "all" } }
                : new object[] { new { name = "customer", required = true } }
        };

    private static object ClientValues() => new { parameters = new { customer_id = "42" } };

    private async Task<JsonObject> Preview(Guid id, object values)
        => await (await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", values)).Content.ReadFromJsonAsync<JsonObject>();

    private static JsonNode Field(JsonObject preview, string label)
        => preview["fields"]!.AsArray().Single(field => field!["label"]!.GetValue<string>() == label);
}
