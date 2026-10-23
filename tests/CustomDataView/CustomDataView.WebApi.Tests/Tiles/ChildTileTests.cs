using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.WebApi.Tests.Tiles;

public class ChildTileTests : TileApiTests
{
    private const string ClientUrl = "https://api.example.com/clients/42";
    private const string OrdersUrl = "https://api.example.com/orders?customer=7";

    private Guid clientConnectionId;
    private Guid ordersConnectionId;
    private Guid carrierConnectionId;
    private Guid ordersTileId;
    private Guid carrierTileId;
    private Guid shippingTileId;
    private Guid filterTileId;
    private Guid customerTileId;

    [SetUp]
    public async Task CreateTiles()
    {
        Factory.ExternalApi.Respond = (request, _) => Task.FromResult(request.RequestUri!.AbsoluteUri switch
        {
            ClientUrl => Json("""{"id": 7, "name": "acme", "country": "PL"}"""),
            OrdersUrl => Json("""{"count": 3, "status": "sent"}"""),
            "https://api.example.com/orders?customer=7&status=new" => Json("""{"count": 1, "status": "new"}"""),
            "https://api.example.com/carriers?country=PL" => Json("""{"name": "DHL"}"""),
            "https://api.example.com/carriers?country=DE" => Json("""{"name": "DPD"}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        clientConnectionId = await CreateConnection(new
        {
            name = "Klient",
            method = "GET",
            url = "https://api.example.com/clients/{customer_id}",
            pathParameters = new[] { new { name = "customer_id", required = true } }
        });
        ordersConnectionId = await CreateConnection(new
        {
            name = "Zamówienia",
            method = "GET",
            url = "https://api.example.com/orders",
            queryParameters = new object[] { new { name = "customer", required = true }, new { name = "status", required = false } }
        });
        carrierConnectionId = await CreateConnection(new
        {
            name = "Przewoźnik",
            method = "GET",
            url = "https://api.example.com/carriers",
            queryParameters = new[] { new { name = "country", required = true } }
        });
        ordersTileId = await CreateTile(DataTile("Zamówienia", ("orders", ordersConnectionId), Data("orders", "$.count", "Liczba")));
        carrierTileId = await CreateTile(DataTile("Przewoźnik", ("carrier", carrierConnectionId), Data("carrier", "$.name", "Nazwa przewoźnika")));
        var shipping = DataTile("Dostawa", ("orders", ordersConnectionId), Data("orders", "$.status", "Status"));
        shipping["children"] = new JsonArray(Child("carrier", carrierTileId, 0));
        shippingTileId = await CreateTile(shipping);
        filterTileId = await CreateTile(Form("Filtr", required: false));
        customerTileId = await CreateTile(DataTile(
            "Klient",
            ("client", clientConnectionId),
            Data("client", "$.id", "ID"),
            Data("client", "$.name", "Nazwa"),
            Data("client", "$.country", "Kraj")));
    }

    [Test(Description = "scenario_TEMP-036 - #1")]
    public async Task Given_TileWithFieldsAndEagerAndLazyChildren_When_UserPreviewsIt_Then_ChildrenHaveAddressesStatesAndNestedPreview()
    {
        var id = await CreateTile(Customer360());

        var preview = await Preview(id, new { parameters = new { customer_id = "42" } });

        Assert.That(preview["fields"]!.AsArray().Select(field => field!["label"]!.GetValue<string>()), Is.EqualTo(new[] { "Nazwa", "ID" }));
        var children = preview["children"]!.AsArray();
        Assert.That(
            JsonNode.DeepEquals(
                children[0],
                JsonNode.Parse($$"""
                                {"key": "lazy-orders", "address": "lazy-orders", "tileId": "{{ordersTileId}}", "position": 0, "cssClass": null,
                                 "state": "awaiting_load", "inputs": {"parameters": {"customer": 7} } }
                                """)),
            Is.True,
            children[0]?.ToJsonString());
        Assert.That(children[1]!["address"]?.GetValue<string>(), Is.EqualTo("orders"));
        Assert.That(children[1]!["cssClass"]?.GetValue<string>(), Is.EqualTo("col-6"));
        Assert.That(children[1]!["state"]?.GetValue<string>(), Is.EqualTo("data"));
        Assert.That(children[1]!["tile"]!["name"]?.GetValue<string>(), Is.EqualTo("Zamówienia"));
        Assert.That(JsonNode.DeepEquals(children[1]!["tile"]!["fields"], JsonNode.Parse("""[{"label": "Liczba", "value": 3}]""")), Is.True);
        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url), Is.EqualTo(new[] { ClientUrl, OrdersUrl }));
    }

    [Test(Description = "scenario_TEMP-036 - #2")]
    public async Task Given_FormChildBoundToParentSource_When_UserPreviewsWithFormValue_Then_ParentSourceIsFiltered()
    {
        var id = await CreateTile(FilteredOrders(filterTileId));

        var preview = await Preview(id, new
        {
            parameters = new { customer = "7" },
            forms = new Dictionary<string, object> { ["filter"] = new Dictionary<string, object> { ["Status"] = "new" } }
        });

        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url), Is.EqualTo(new[] { "https://api.example.com/orders?customer=7&status=new" }));
        var filter = preview["children"]![0]!;
        Assert.That(filter["state"]?.GetValue<string>(), Is.EqualTo("data"));
        Assert.That(filter["tile"]!["fields"]![0]!["value"]?.GetValue<string>(), Is.EqualTo("new"));
    }

    [Test(Description = "scenario_TEMP-036 - #3")]
    public async Task Given_ChildToChildAndGrandchildBindings_When_UserPreviewsTile_Then_GrandchildGetsValueThroughPath()
    {
        var id = await CreateTile(CustomerAndShipping(shippingTileId));

        var preview = await Preview(id, new { descendants = new Dictionary<string, object> { ["customer"] = new { parameters = new { customer_id = "42" } } } });

        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url), Does.Contain("https://api.example.com/carriers?country=PL"));
        var carrier = preview["children"]![1]!["tile"]!["children"]![0]!;
        Assert.That(carrier["address"]?.GetValue<string>(), Is.EqualTo("shipping/carrier"));
        Assert.That(carrier["state"]?.GetValue<string>(), Is.EqualTo("data"), carrier.ToJsonString());
    }

    [Test(Description = "scenario_TEMP-036 - #4")]
    public async Task Given_ShippingTile_When_UserPreviewsWithValuesForCarrierDescendant_Then_CarrierGetsDeliveredCountry()
    {
        var preview = await Preview(shippingTileId, new
        {
            parameters = new { customer = "7" },
            descendants = new Dictionary<string, object> { ["carrier"] = new { parameters = new { country = "DE" } } }
        });

        Assert.That(Factory.ExternalApi.Requests.Select(request => request.Url), Does.Contain("https://api.example.com/carriers?country=DE"));
        Assert.That(preview["children"]![0]!["tile"]!["fields"]![0]!["value"]?.GetValue<string>(), Is.EqualTo("DPD"));
    }

    [TestCase("repeated-key", "children[1].key")]
    [TestCase("key-of-source", "children[0].key")]
    [TestCase("repeated-position", "children[1].position")]
    [TestCase("missing-tile", "children[0].tileId")]
    [TestCase("item-without-items", "children[0].scope")]
    [TestCase("unknown-load", "children[0].load")]
    [TestCase("long-css-class", "children[0].cssClass")]
    [Test(Description = "scenario_TEMP-036 - #5")]
    public async Task Given_TileWithInvalidChild_When_UserSavesIt_Then_SaveIsRejectedWithChildError(string invalid, string errorKey)
    {
        var tile = DataTile("Klient", ("client", clientConnectionId), Data("client", "$.name", "Nazwa"));
        tile["children"] = new JsonArray(Child("orders", ordersTileId, 0), Child("carrier", carrierTileId, 1));
        var children = tile["children"]!.AsArray();
        switch (invalid)
        {
            case "repeated-key":
                children[1]!["key"] = "orders";
                break;
            case "key-of-source":
                children[0]!["key"] = "client";
                break;
            case "repeated-position":
                children[1]!["position"] = 0;
                break;
            case "missing-tile":
                children[0]!["tileId"] = Guid.NewGuid();
                break;
            case "item-without-items":
                children[0]!["scope"] = "item";
                break;
            case "unknown-load":
                children[0]!["load"] = "later";
                break;
            case "long-css-class":
                children[0]!["cssClass"] = new string('c', 201);
                break;
        }

        await AssertRejected(await Client.PostAsJsonAsync(TilesUrl, tile), errorKey);
    }

    [Test(Description = "scenario_TEMP-036 - #6")]
    public async Task Given_NestedTiles_When_UserCreatesCycle_Then_SaveIsRejected()
    {
        var carrier = DataTile("Przewoźnik", ("carrier", carrierConnectionId), Data("carrier", "$.name", "Nazwa przewoźnika"));
        carrier["children"] = new JsonArray(Child("shipping", shippingTileId, 0));

        var cycle = await Client.PutAsJsonAsync($"{TilesUrl}/{carrierTileId}", carrier);

        await AssertRejected(cycle, "children[0].tileId");
    }

    [TestCase("parameter-outside-contract", "bindings[1].map[0].parameter")]
    [TestCase("field-outside-outputs", "bindings[1].map[0].field")]
    [TestCase("missing-grandchild", "bindings[0].to")]
    [TestCase("bound-by-intermediate", "bindings[0].map[0].parameter")]
    [TestCase("parent-to-source", "bindings[0].to")]
    [TestCase("missing-great-grandchild", "bindings[0].to")]
    [Test(Description = "scenario_TEMP-036 - #7")]
    public async Task Given_TileWithInvalidChildBinding_When_UserSavesIt_Then_SaveIsRejectedWithBindingError(string invalid, string errorKey)
    {
        var tile = CustomerAndShipping(invalid == "bound-by-intermediate" ? await ShippingWithBoundCarrier() : shippingTileId);
        var bindings = tile["bindings"]!.AsArray();
        switch (invalid)
        {
            case "parameter-outside-contract":
                bindings[1]!["map"]![0]!["parameter"] = "region";
                break;
            case "field-outside-outputs":
                bindings[1]!["map"]![0]!["field"] = "Telefon";
                break;
            case "missing-grandchild":
                bindings[0]!["to"] = "shipping/courier";
                break;
            case "parent-to-source":
                tile["sources"] = new JsonArray(new JsonObject { ["key"] = "orders", ["kind"] = "connection", ["connectionId"] = ordersConnectionId });
                tile["fields"] = new JsonArray(Data("orders", "$.count", "Liczba"));
                bindings[0] = Binding("parent", "orders", ("Liczba", "customer"));
                break;
            case "missing-great-grandchild":
                bindings[0]!["to"] = "shipping/carrier/x";
                break;
        }

        await AssertRejected(await Client.PostAsJsonAsync(TilesUrl, tile), errorKey);
    }

    [Test(Description = "scenario_TEMP-036 - #8")]
    public async Task Given_ParentSourceUnavailableOrRequiredFormFieldEmpty_When_UserPreviews_Then_TargetsWaitOrFailWithCause()
    {
        var customer360 = await CreateTile(Customer360());
        var filtered = await CreateTile(FilteredOrders(await CreateTile(Form("Filtr wymagany", required: true))));
        var respond = Factory.ExternalApi.Respond;
        Factory.ExternalApi.Respond = (request, token) => request.RequestUri!.AbsoluteUri == ClientUrl
            ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
            : respond(request, token);

        var failed = await Preview(customer360, new { parameters = new { customer_id = "42" } });
        var waiting = await Preview(filtered, new { parameters = new { customer = "7" } });

        Assert.That(
            JsonNode.DeepEquals(
                failed["children"]![1]!["error"],
                JsonNode.Parse("""{"code": "dependency_failed", "cause": {"node": "customer", "field": "ID", "code": "source_unavailable"}}""")),
            Is.True,
            failed["children"]![1]!.ToJsonString());
        Assert.That(
            JsonNode.DeepEquals(
                waiting["fields"]![0]!["error"],
                JsonNode.Parse("""{"code": "awaiting_input", "pending": [{"from": "filter", "field": "Status", "parameter": "status"}]}""")),
            Is.True,
            waiting["fields"]![0]!.ToJsonString());
    }

    [Test(Description = "scenario_TEMP-036 - #9")]
    public async Task Given_CarrierEmbeddedInShipping_When_UserDeletesCarrier_Then_DeletionIsRejectedWithParent()
    {
        var response = await Client.DeleteAsync($"{TilesUrl}/{carrierTileId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        var conflict = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(
            JsonNode.DeepEquals(
                conflict,
                JsonNode.Parse($$"""{"code": "tile_in_use", "tiles": [{"id": "{{shippingTileId}}", "name": "Dostawa"}]}""")),
            Is.True,
            conflict.ToJsonString());
        Assert.That((await Client.GetAsync($"{TilesUrl}/{carrierTileId}")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [TestCase("rename", HttpStatusCode.Conflict)]
    [TestCase("remove", HttpStatusCode.Conflict)]
    [TestCase("path", HttpStatusCode.NoContent)]
    [Test(Description = "scenario_TEMP-036 - #10")]
    public async Task Given_ParentBindsCustomerCountry_When_UserEditsCountryField_Then_OnlyLabelChangesAreBlocked(string change, HttpStatusCode expected)
    {
        var parentId = await CreateTile(CustomerAndShipping(shippingTileId));
        var customer = DataTile(
            "Klient",
            ("client", clientConnectionId),
            Data("client", "$.id", "ID"),
            Data("client", "$.name", "Nazwa"),
            Data("client", change == "path" ? "$.address.country" : "$.country", change == "rename" ? "Państwo" : "Kraj"));
        if (change == "remove")
        {
            customer["fields"]!.AsArray().RemoveAt(2);
        }

        var response = await Client.PutAsJsonAsync($"{TilesUrl}/{customerTileId}", customer);

        Assert.That(response.StatusCode, Is.EqualTo(expected));
        if (expected == HttpStatusCode.Conflict)
        {
            var conflict = await response.Content.ReadFromJsonAsync<JsonObject>();
            Assert.That(conflict["code"]?.GetValue<string>(), Is.EqualTo("field_in_use"));
            Assert.That(conflict["fields"]!.AsArray().Select(field => field!.GetValue<string>()), Is.EqualTo(new[] { "Kraj" }));
            Assert.That(conflict["tiles"]!.AsArray().Select(tile => tile!["id"]!.GetValue<Guid>()), Is.EqualTo(new[] { parentId }));
        }
    }

    [Test(Description = "scenario_TEMP-036 - #11")]
    public async Task Given_OrdersTileNoLongerDeclaresCustomer_When_UserPreviewsParent_Then_ChildHasInvalidBinding()
    {
        var id = await CreateTile(Customer360());
        var simpleOrders = await CreateConnection(new { name = "Zamówienia v2", method = "GET", url = "https://api.example.com/orders/all" });
        (await Client.PutAsJsonAsync($"{TilesUrl}/{ordersTileId}", DataTile("Zamówienia", ("orders", simpleOrders), Data("orders", "$.count", "Liczba"))))
            .EnsureSuccessStatusCode();

        var preview = await Preview(id, new { parameters = new { customer_id = "42" } });

        var orders = preview["children"]![1]!;
        Assert.That(orders["state"]?.GetValue<string>(), Is.EqualTo("error"));
        Assert.That(orders["error"]!["code"]?.GetValue<string>(), Is.EqualTo("invalid_binding"));
        Assert.That(orders["error"]!["parameters"]![0]!["name"]?.GetValue<string>(), Is.EqualTo("customer"));
    }

    private JsonObject Customer360()
    {
        var tile = DataTile("Klient 360", ("customer", clientConnectionId), Data("customer", "$.name", "Nazwa"), Data("customer", "$.id", "ID", hidden: true));
        tile["children"] = new JsonArray(Child("orders", ordersTileId, 1, "col-6"), Child("lazy-orders", ordersTileId, 0, load: "lazy"));
        tile["bindings"] = new JsonArray(Binding("parent", "orders", ("ID", "customer")), Binding("parent", "lazy-orders", ("ID", "customer")));
        return tile;
    }

    private JsonObject FilteredOrders(Guid formId)
    {
        var tile = DataTile("Zamówienia z filtrem", ("orders", ordersConnectionId), Data("orders", "$.count", "Liczba"));
        tile["children"] = new JsonArray(Child("filter", formId, 0));
        tile["bindings"] = new JsonArray(Binding("filter", "orders", ("Status", "status")));
        return tile;
    }

    private JsonObject CustomerAndShipping(Guid shipping)
    {
        var tile = DataTile("Klient i dostawa", ("client", clientConnectionId), Data("client", "$.name", "Nazwa", hidden: true));
        tile["children"] = new JsonArray(Child("customer", customerTileId, 0), Child("shipping", shipping, 1));
        tile["bindings"] = new JsonArray(Binding("customer", "shipping/carrier", ("Kraj", "country")), Binding("customer", "shipping", ("ID", "customer")));
        return tile;
    }

    private async Task<Guid> ShippingWithBoundCarrier()
    {
        var shipping = DataTile("Dostawa PL", ("orders", ordersConnectionId), Data("orders", "$.status", "Status"), Data("orders", "$.country", "Kraj dostawy", hidden: true));
        shipping["children"] = new JsonArray(Child("carrier", carrierTileId, 0));
        shipping["bindings"] = new JsonArray(Binding("parent", "carrier", ("Kraj dostawy", "country")));
        return await CreateTile(shipping);
    }

    private static JsonObject DataTile(string name, (string Key, Guid ConnectionId) source, params JsonObject[] fields)
        => new()
        {
            ["name"] = name,
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = source.Key, ["kind"] = "connection", ["connectionId"] = source.ConnectionId }),
            ["fields"] = new JsonArray(fields.Cast<JsonNode>().ToArray())
        };

    private static JsonObject Form(string name, bool required)
        => new()
        {
            ["name"] = name,
            ["display"] = "form",
            ["fields"] = new JsonArray(new JsonObject { ["label"] = "Status", ["type"] = "text", ["required"] = required })
        };

    private static JsonObject Data(string source, string path, string label, bool hidden = false)
        => new() { ["source"] = source, ["path"] = path, ["label"] = label, ["hidden"] = hidden };

    private static JsonObject Child(string key, Guid tileId, int position, string cssClass = null, string load = "eager")
        => new() { ["key"] = key, ["tileId"] = tileId, ["position"] = position, ["cssClass"] = cssClass, ["scope"] = "tile", ["load"] = load };

    private static JsonObject Binding(string from, string to, params (string Field, string Parameter)[] map)
        => new()
        {
            ["from"] = from,
            ["to"] = to,
            ["map"] = new JsonArray(map.Select(mapping => (JsonNode)new JsonObject { ["field"] = mapping.Field, ["parameter"] = mapping.Parameter }).ToArray())
        };

    private async Task<JsonObject> Preview(Guid id, object values)
        => await (await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", values)).Content.ReadFromJsonAsync<JsonObject>();

    private static async Task AssertRejected(HttpResponseMessage response, string errorKey)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problem["errors"]!.AsObject().Select(error => error.Key), Does.Contain(errorKey), problem.ToJsonString());
    }
}
