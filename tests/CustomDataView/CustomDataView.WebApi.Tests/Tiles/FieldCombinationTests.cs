using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace CustomDataView.WebApi.Tests.Tiles;

public class FieldCombinationTests : TileApiTests
{
    private const string Dir = "scenario_TEMP-044";

    private const string CitiesResponse =
        """
        {"cities": [{"id": 1, "name": "Kraków", "country": "PL"}, {"id": 2, "name": "Praga", "country": "CZ"}]}
        """;

    private const string PlaceResponse =
        """
        {"street": "Długa", "number": "5", "city": "Kraków", "lat": 50.0647, "lon": 19.9461,
         "email": null, "phone": "123 456 789", "tags": ["zabytki", null, "rynek"]}
        """;

    private Guid placeConnectionId;
    private Guid citiesConnectionId;

    [SetUp]
    public async Task CreatePlaceConnection()
    {
        Factory.ExternalApi.Respond = (request, _) => Task.FromResult(request.RequestUri!.AbsolutePath switch
        {
            "/places/1" => Json(PlaceResponse),
            "/cities" => Json(CitiesResponse),
            "/weather" => Json("""{"temp": 12.5}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        placeConnectionId = await CreateConnection(new { name = "Miejsce", method = "GET", url = "https://api.example.com/places/1" });
        citiesConnectionId = await CreateConnection(new { name = "Miasta", method = "GET", url = "https://api.example.com/cities" });
    }

    [Test(Description = $"{Dir} - #2")]
    public async Task Given_TemplateFromHiddenFields_When_UserPreviewsTile_Then_FieldHoldsComposedAndTransformedText()
    {
        var tile = Address(
            Combined("Pełny adres", "template", "{Ulica} {Nr}, {Miasto}"),
            Combined("Adres wielkimi", "template", "{ulica} {Nr}, {Miasto}", transformations: [Transformation("uppercase")]));
        var id = await CreateTile(tile);

        var preview = await Preview(id);

        Assert.That(Value(preview, "Pełny adres"), Is.EqualTo("Długa 5, Kraków"), preview.ToJsonString());
        Assert.That(Value(preview, "Adres wielkimi"), Is.EqualTo("DŁUGA 5, KRAKÓW"));
        Assert.That(
            new[] { "Ulica", "Nr", "Miasto" }.Select(label => Field(preview, label)["hidden"]?.GetValue<bool>()),
            Has.All.True);
    }

    [Test(Description = $"{Dir} - #3")]
    public async Task Given_TemplateWithMissingValueAndDoubledBraces_When_UserPreviewsTile_Then_MissingValueIsEmptyAndBracesAreLiteral()
    {
        RespondWithPlace(place => place["number"] = null);
        var id = await CreateTile(Address(Combined("Etykieta", "template", "{{{Ulica} {Nr}}}")));

        var preview = await Preview(id);

        Assert.That(Value(preview, "Etykieta"), Is.EqualTo("{Długa }"), preview.ToJsonString());
    }

    [TestCase("Ulica|Nr|Miasto", "Długa, 5, Kraków")]
    [TestCase("Miasto|Tagi", "Kraków, zabytki, rynek")]
    [TestCase("Lat|Lon", "50.06, 19.95")]
    [Test(Description = $"{Dir} - #4")]
    public async Task Given_JoinOfFields_When_UserPreviewsTile_Then_ValuesAreJoinedSkippingMissingAndSpreadingLists(string fields, string expected)
    {
        var id = await CreateTile(Address(PlaceField("$.tags[*]", "Tagi"), Combined("Złączone", "join", ", ", fields.Split('|'))));

        var preview = await Preview(id);

        Assert.That(Value(preview, "Złączone"), Is.EqualTo(expected), preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #5")]
    public async Task Given_JoinOfMissingValuesAndOfObject_When_UserPreviewsTile_Then_FirstHasNoValueAndSecondHasCombinationError()
    {
        var id = await CreateTile(Address(
            PlaceField("$.email", "Email"),
            Combined("Bez wartości", "join", fields: ["Email"]),
            PlaceField("$", "Całość"),
            Combined("Błędne", "join", fields: ["Miasto", "Całość"])));

        var preview = await Preview(id);

        Assert.That(
            JsonNode.DeepEquals(Field(preview, "Bez wartości"), JsonNode.Parse("""{"label": "Bez wartości", "value": null}""")),
            Is.True,
            preview.ToJsonString());
        Assert.That(
            JsonNode.DeepEquals(
                Field(preview, "Błędne"),
                JsonNode.Parse("""{"label": "Błędne", "error": {"code": "combination_failed", "combination": "join", "reason": "unsupported_value"}}""")),
            Is.True,
            preview.ToJsonString());
        Assert.That(Value(preview, "Miasto"), Is.EqualTo("Kraków"));
    }

    [Test(Description = $"{Dir} - #6")]
    public async Task Given_ObjectBoundToChildDataInput_When_UserPreviewsTile_Then_ChildShowsMembersOfObject()
    {
        var mapId = await CreateTile(new JsonObject
        {
            ["name"] = "Mapa",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "point", ["kind"] = "input" }),
            ["fields"] = new JsonArray(
                new JsonObject { ["source"] = "point", ["path"] = "$.Lat", ["label"] = "Szerokość" },
                new JsonObject { ["source"] = "point", ["path"] = "$.Lon", ["label"] = "Długość" })
        });
        var tile = Address(Combined("Punkt", "object", fields: ["Lat", "Lon"], hidden: true));
        tile["children"] = new JsonArray(new JsonObject { ["key"] = "map", ["tileId"] = mapId, ["position"] = 0 });
        tile["bindings"] = new JsonArray(new JsonObject
        {
            ["from"] = "parent",
            ["to"] = "map",
            ["map"] = new JsonArray(new JsonObject { ["field"] = "Punkt", ["input"] = "point" })
        });
        var id = await CreateTile(tile);
        Factory.ExternalApi.Respond = async (_, cancellationToken) =>
        {
            await Task.Delay(50, cancellationToken);
            return Json(PlaceResponse);
        };

        var preview = await Preview(id);

        Assert.That(
            JsonNode.DeepEquals(Field(preview, "Punkt")["value"], JsonNode.Parse("""{"Lat": 50.06, "Lon": 19.95}""")),
            Is.True,
            preview.ToJsonString());
        var map = preview["children"]![0]!["tile"]!;
        Assert.That(
            map["fields"]!.AsArray().Select(field => (field!["label"]!.GetValue<string>(), field["value"]?.GetValue<decimal>())),
            Is.EqualTo(new (string, decimal?)[] { ("Szerokość", 50.06m), ("Długość", 19.95m) }),
            preview.ToJsonString());
    }

    [TestCase("Email|Telefon", "123 456 789")]
    [TestCase("Telefon|Email", "123 456 789")]
    [TestCase("Email|Email2", null)]
    [Test(Description = $"{Dir} - #7")]
    public async Task Given_FirstAvailableValueOfFields_When_UserPreviewsTile_Then_FirstValueOtherThanMissingIsReturned(string fields, string expected)
    {
        var id = await CreateTile(Address(
            PlaceField("$.email", "Email"),
            PlaceField("$.phone", "Telefon"),
            PlaceField("$.email", "Email2"),
            Combined("Kontakt", "coalesce", fields: fields.Split('|'))));

        var preview = await Preview(id);

        Assert.That(Field(preview, "Kontakt")["error"], Is.Null, preview.ToJsonString());
        Assert.That(Value(preview, "Kontakt"), Is.EqualTo(expected));
    }

    [Test(Description = $"{Dir} - #8")]
    public async Task Given_ListWithCombinedTitleAndItemAction_When_UserPreviewsList_Then_EachItemHasItsTitleAndActionCarriesIt()
    {
        var cityConnectionId = await CreateConnection(new
        {
            name = "Miasto po tytule",
            method = "GET",
            url = "https://api.example.com/city",
            queryParameters = new[] { new { name = "title", required = true } }
        });
        var cityTileId = await CreateTile(new JsonObject
        {
            ["name"] = "Miasto",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "city", ["kind"] = "connection", ["connectionId"] = cityConnectionId }),
            ["fields"] = new JsonArray(new JsonObject { ["source"] = "city", ["path"] = "$.name", ["label"] = "Nazwa" })
        });
        var list = Cities("list", Combined("Tytuł", "template", "{Nazwa} ({Kraj})"));
        list["fields"]!.AsArray().Last()!["role"] = "title";
        list["actions"] = new JsonArray(new JsonObject
        {
            ["key"] = "details",
            ["label"] = "Szczegóły",
            ["tileId"] = cityTileId,
            ["map"] = new JsonArray(new JsonObject { ["field"] = "Tytuł", ["parameter"] = "title" })
        });
        var id = await CreateTile(list);

        var preview = await Preview(id);

        var items = preview["items"]!.AsArray();
        Assert.That(
            items.Select(item => item!["fields"]!.AsArray().Single(field => field!["label"]!.GetValue<string>() == "Tytuł")!["value"]?.GetValue<string>()),
            Is.EqualTo(new[] { "Kraków (PL)", "Praga (CZ)" }),
            preview.ToJsonString());
        Assert.That(items[1]!["actions"]![0]!["inputs"]!["parameters"]!["title"]?.GetValue<string>(), Is.EqualTo("Praga (CZ)"));
    }

    [Test(Description = $"{Dir} - #8")]
    public async Task Given_CombinedItemFieldBoundToItemChild_When_UserPreviewsList_Then_EachItemChildGetsItsComposedValue()
    {
        var weatherTileId = await CreateWeatherTile();
        var list = Cities("list", Combined("Tytuł", "template", "{Nazwa} ({Kraj})"));
        list["fields"]!.AsArray().Last()!["role"] = "title";
        list["children"] = new JsonArray(new JsonObject { ["key"] = "weather", ["tileId"] = weatherTileId, ["position"] = 0, ["scope"] = "item" });
        list["bindings"] = new JsonArray(new JsonObject
        {
            ["from"] = "parent",
            ["to"] = "weather",
            ["map"] = new JsonArray(new JsonObject { ["field"] = "Tytuł", ["parameter"] = "latlon", ["scope"] = "item" })
        });
        var id = await CreateTile(list);

        var preview = await Preview(id);

        Assert.That(
            Factory.ExternalApi.Requests
                .Where(request => request.Url.Contains("/weather"))
                .Select(request => HttpUtility.ParseQueryString(new Uri(request.Url).Query)["latlon"]),
            Is.EquivalentTo(new[] { "Kraków (PL)", "Praga (CZ)" }),
            preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #9")]
    public async Task Given_TemplateBoundToChildConnectionParameter_When_UserPreviewsTile_Then_ChildConnectionIsCalledWithComposedValue()
    {
        var weatherTileId = await CreateWeatherTile();
        var tile = Address(Combined("Współrzędne", "template", "{Lat},{Lon}"));
        tile["children"] = new JsonArray(new JsonObject { ["key"] = "weather", ["tileId"] = weatherTileId, ["position"] = 0 });
        tile["bindings"] = new JsonArray(new JsonObject
        {
            ["from"] = "parent",
            ["to"] = "weather",
            ["map"] = new JsonArray(new JsonObject { ["field"] = "Współrzędne", ["parameter"] = "latlon" })
        });
        var id = await CreateTile(tile);

        var preview = await Preview(id);

        Assert.That(LatLonOfWeatherRequest(), Is.EqualTo("50.06,19.95"), preview.ToJsonString());
        Assert.That(preview["children"]![0]!["tile"]!["fields"]![0]!["value"]?.GetValue<decimal>(), Is.EqualTo(12.5m));
    }

    private void RespondWithPlace(Action<JsonObject> change)
    {
        var place = JsonNode.Parse(PlaceResponse)!.AsObject();
        change(place);
        Factory.ExternalApi.Respond = (_, _) => Task.FromResult(Json(place.ToJsonString()));
    }

    [TestCase("unavailable-source", "Ulica", "source_unavailable")]
    [TestCase("missing-path", "Miasto", "path_not_found")]
    [Test(Description = $"{Dir} - #10")]
    public async Task Given_FailingInputField_When_UserPreviewsTile_Then_CombinedFieldHasDependencyErrorAndOtherFieldsRemain(
        string situation,
        string field,
        string cause)
    {
        var tile = Address(Combined("Pełny adres", "template", "{Ulica} {Nr}, {Miasto}"));
        if (situation == "missing-path")
        {
            tile["fields"]![2]!["path"] = "$.town";
        }
        else
        {
            Factory.ExternalApi.Respond = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }

        var id = await CreateTile(tile);

        var preview = await Preview(id);

        Assert.That(
            JsonNode.DeepEquals(
                Field(preview, "Pełny adres")["error"],
                JsonNode.Parse($$$"""{"code": "dependency_failed", "cause": {"node": "api", "field": "{{{field}}}", "code": "{{{cause}}}"}}""")),
            Is.True,
            preview.ToJsonString());
        if (situation == "missing-path")
        {
            Assert.That(Value(preview, "Ulica"), Is.EqualTo("Długa"));
        }
    }

    [TestCase("unknown-key", "fields[5].combine.key")]
    [TestCase("source-and-combine", "fields[5]")]
    [TestCase("unknown-field", "fields[5].combine")]
    [TestCase("self", "fields[5].combine")]
    [TestCase("combined-input", "fields[6].combine")]
    [TestCase("single-coalesce", "fields[6].combine.fields")]
    [TestCase("unclosed-brace", "fields[5].combine.parameter")]
    [TestCase("no-label", "fields[5].combine.parameter")]
    [TestCase("template-with-fields", "fields[5].combine.fields")]
    [TestCase("object-with-parameter", "fields[5].combine.parameter")]
    [TestCase("object-with-transformation", "fields[5].transformations")]
    [TestCase("template-with-list", "fields[6].combine")]
    [Test(Description = $"{Dir} - #11")]
    public async Task Given_InvalidCombinedField_When_UserSavesTile_Then_SaveIsRejectedWithCombinedFieldError(string problem, string error)
    {
        JsonObject[] fields = problem switch
        {
            "unknown-key" => [Combined("X", "concat", fields: ["Ulica"])],
            "source-and-combine" => [Combined("X", "join", fields: ["Ulica"])],
            "unknown-field" => [Combined("X", "join", fields: ["Kod pocztowy"])],
            "self" => [Combined("X", "join", fields: ["X"])],
            "combined-input" => [Combined("Y", "join", fields: ["Ulica"]), Combined("X", "join", fields: ["Y"])],
            "single-coalesce" => [PlaceField("$.email", "Email"), Combined("X", "coalesce", fields: ["Email"])],
            "unclosed-brace" => [Combined("X", "template", "{Ulica} {Nr")],
            "no-label" => [Combined("X", "template", "Adres")],
            "template-with-fields" => [Combined("X", "template", "{Ulica}", ["Nr"])],
            "object-with-parameter" => [Combined("X", "object", "x", ["Lat"])],
            "object-with-transformation" => [Combined("X", "object", fields: ["Lat"], transformations: [Transformation("uppercase")])],
            _ => [PlaceField("$.tags[*]", "Tagi"), Combined("X", "template", "{Tagi}")]
        };
        var tile = Address(fields);
        if (problem == "source-and-combine")
        {
            tile["fields"]![5]!["source"] = "api";
            tile["fields"]![5]!["path"] = "$.street";
        }

        var response = await Client.PostAsJsonAsync(TilesUrl, tile);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problemDetails = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problemDetails!["errors"]!.AsObject().Select(pair => pair.Key), Is.EqualTo(new[] { error }), problemDetails.ToJsonString());
    }

    [TestCase("input-from-other-source", "fields[4].combine")]
    [TestCase("combined-item-key", "items.key")]
    [TestCase("source-binding", "bindings[0].map[0].field")]
    [Test(Description = $"{Dir} - #12")]
    public async Task Given_CombinedFieldBreakingItemRules_When_UserSavesTile_Then_SaveIsRejected(string problem, string error)
    {
        var detailsConnectionId = await CreateConnection(new
        {
            name = "Szczegóły miasta",
            method = "GET",
            url = "https://api.example.com/city",
            queryParameters = new[] { new { name = "title", required = false } }
        });
        var list = Cities(
            "list",
            new JsonObject { ["source"] = "place", ["path"] = "$.street", ["label"] = "Ulica", ["hidden"] = true },
            Combined("Tytuł", "template", problem == "input-from-other-source" ? "{Nazwa} {Ulica}" : "{Nazwa} ({Kraj})"));
        list["fields"]!.AsArray().Last()!["role"] = "title";
        list["sources"]!.AsArray().Add(new JsonObject { ["key"] = "place", ["kind"] = "connection", ["connectionId"] = placeConnectionId });
        list["sources"]!.AsArray().Add(new JsonObject { ["key"] = "detail", ["kind"] = "connection", ["connectionId"] = detailsConnectionId });
        if (problem == "combined-item-key")
        {
            list["items"]!["key"] = "Tytuł";
        }

        if (problem == "source-binding")
        {
            list["bindings"] = new JsonArray(new JsonObject
            {
                ["from"] = "cities",
                ["to"] = "detail",
                ["map"] = new JsonArray(new JsonObject { ["field"] = "Tytuł", ["parameter"] = "title" })
            });
        }

        var response = await Client.PostAsJsonAsync(TilesUrl, list);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problemDetails = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(problemDetails!["errors"]!.AsObject().Select(pair => pair.Key), Is.EqualTo(new[] { error }), problemDetails.ToJsonString());
    }

    [Test(Description = $"{Dir} - #13")]
    public async Task Given_FieldUsedByCombination_When_UserRenamesOrRemovesIt_Then_SaveIsRejectedForCombinedField()
    {
        var tile = Address(Combined("Pełny adres", "template", "{Ulica} {Nr}, {Miasto}"));
        var id = await CreateTile(tile);
        var renamed = tile.DeepClone().AsObject();
        renamed["fields"]![2]!["label"] = "Miejscowość";
        var removed = tile.DeepClone().AsObject();
        removed["fields"]!.AsArray().RemoveAt(0);

        var renameResponse = await Client.PutAsJsonAsync($"{TilesUrl}/{id}", renamed);
        var removeResponse = await Client.PutAsJsonAsync($"{TilesUrl}/{id}", removed);

        Assert.That(renameResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(
            (await renameResponse.Content.ReadFromJsonAsync<JsonObject>())!["errors"]!.AsObject().Select(pair => pair.Key),
            Is.EqualTo(new[] { "fields[5].combine" }));
        Assert.That(removeResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(
            (await removeResponse.Content.ReadFromJsonAsync<JsonObject>())!["errors"]!.AsObject().Select(pair => pair.Key),
            Is.EqualTo(new[] { "fields[4].combine" }));
        Assert.That(Field(await Preview(id), "Pełny adres")["value"]?.GetValue<string>(), Is.EqualTo("Długa 5, Kraków"));
    }

    [Test(Description = $"{Dir} - #14")]
    public async Task Given_CombinedFieldBoundByParent_When_UserRemovesIt_Then_EditIsBlockedWithFieldInUse()
    {
        var weatherTileId = await CreateWeatherTile();
        var address = Address(Combined("Współrzędne", "template", "{Lat},{Lon}"));
        var addressId = await CreateTile(address);
        var panelId = await CreateTile(Panel(addressId, weatherTileId));
        address["fields"]!.AsArray().RemoveAt(5);

        var response = await Client.PutAsJsonAsync($"{TilesUrl}/{addressId}", address);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        var conflict = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(conflict!["code"]?.GetValue<string>(), Is.EqualTo("field_in_use"));
        Assert.That(conflict["fields"]!.AsArray().Select(field => field!.GetValue<string>()), Is.EqualTo(new[] { "Współrzędne" }));
        Assert.That(conflict["tiles"]!.AsArray().Select(tile => tile!["id"]!.GetValue<Guid>()), Is.EqualTo(new[] { panelId }));
    }

    [Test(Description = $"{Dir} - #15")]
    public async Task Given_TileWithJoinField_When_UserReadsDetails_Then_FieldHasCombinationAndContractExposesIt()
    {
        var id = await CreateTile(Address(Combined("Miejsce", "join", ", ", ["Ulica", "Miasto"])));

        var tile = await GetTile(id);

        var field = tile["fields"]![5]!;
        Assert.That(
            JsonNode.DeepEquals(field["combine"], JsonNode.Parse("""{"key": "join", "parameter": ", ", "fields": ["Ulica", "Miasto"]}""")),
            Is.True,
            tile.ToJsonString());
        Assert.That(field["source"], Is.Null);
        Assert.That(field["path"], Is.Null);
        Assert.That(tile["contract"]!["outputs"]!["fields"]!.AsArray().Select(label => label!.GetValue<string>()), Does.Contain("Miejsce"));
    }

    [Test(Description = $"{Dir} - #16")]
    public async Task Given_UnsavedTileWithTemplate_When_UserPreviewsDefinition_Then_CombinedFieldIsComposed()
    {
        var tile = Address(Combined("Pełny adres", "template", "{Ulica} {Nr}, {Miasto}"));

        var response = await Client.PostAsJsonAsync($"{TilesUrl}/preview", new { tile });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var preview = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(Value(preview!, "Pełny adres"), Is.EqualTo("Długa 5, Kraków"), preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #17")]
    public async Task Given_ChildCombinedFieldBoundToSibling_When_UserPreviewsPanel_Then_SiblingIsCalledWithComposedValue()
    {
        var weatherTileId = await CreateWeatherTile();
        var addressId = await CreateTile(Address(Combined("Współrzędne", "template", "{Lat},{Lon}")));
        var panelId = await CreateTile(Panel(addressId, weatherTileId));

        var preview = await Preview(panelId);

        Assert.That(LatLonOfWeatherRequest(), Is.EqualTo("50.06,19.95"), preview.ToJsonString());
        var weather = preview["children"]!.AsArray().Single(child => child!["key"]!.GetValue<string>() == "weather")!;
        Assert.That(weather["state"]?.GetValue<string>(), Is.EqualTo("data"));
        Assert.That(weather["tile"]!["fields"]![0]!["value"]?.GetValue<decimal>(), Is.EqualTo(12.5m));
    }

    [Test(Description = $"{Dir} - #18")]
    public async Task Given_TileLinkMappingCombinedField_When_UserPreviewsTile_Then_LinkCarriesComposedValue()
    {
        var mapTileId = await CreateWeatherTile();
        var tile = Address(Combined("Współrzędne", "template", "{Lat},{Lon}"));
        tile["actions"] = new JsonArray(new JsonObject
        {
            ["key"] = "map",
            ["label"] = "Pokaż na mapie",
            ["type"] = "link",
            ["scope"] = "tile",
            ["tileId"] = mapTileId,
            ["map"] = new JsonArray(new JsonObject { ["field"] = "Współrzędne", ["parameter"] = "latlon" })
        });
        var id = await CreateTile(tile);

        var preview = await Preview(id);

        var link = preview["actions"]![0]!;
        Assert.That(link["type"]?.GetValue<string>(), Is.EqualTo("link"), preview.ToJsonString());
        Assert.That(link["inputs"]!["parameters"]!["latlon"]?.GetValue<string>(), Is.EqualTo("50.06,19.95"));
    }

    [Test(Description = $"{Dir} - #19")]
    public async Task Given_TransformationFailingOnCombinedValue_When_UserPreviewsTile_Then_FieldHasTransformationErrorAndOthersRemain()
    {
        var id = await CreateTile(Address(Combined("Pełny adres", "template", "{Ulica} {Nr}, {Miasto}", transformations: [Transformation("round", 2)])));

        var preview = await Preview(id);

        Assert.That(
            JsonNode.DeepEquals(
                Field(preview, "Pełny adres"),
                JsonNode.Parse("""{"label": "Pełny adres", "error": {"code": "transformation_failed", "transformation": "round", "reason": "invalid_value"}}""")),
            Is.True,
            preview.ToJsonString());
        Assert.That(Value(preview, "Ulica"), Is.EqualTo("Długa"));
    }

    [Test(Description = $"{Dir} - #20")]
    public async Task Given_ObjectOfMissingValueAndList_When_UserPreviewsTile_Then_ObjectKeepsNullAndList()
    {
        var id = await CreateTile(Address(
            PlaceField("$.email", "Email"),
            PlaceField("$.tags[*]", "Tagi"),
            Combined("Szczegóły", "object", fields: ["Miasto", "Email", "Tagi"])));

        var preview = await Preview(id);

        Assert.That(
            JsonNode.DeepEquals(
                Field(preview, "Szczegóły")["value"],
                JsonNode.Parse("""{"Miasto": "Kraków", "Email": null, "Tagi": ["zabytki", null, "rynek"]}""")),
            Is.True,
            preview.ToJsonString());
    }

    [TestCase("table")]
    [TestCase("pagination_table")]
    [Test(Description = $"{Dir} - #21")]
    public async Task Given_TableWithCombinedColumn_When_UserPreviewsTable_Then_EachRowHasItsComposedValue(string display)
    {
        var table = Cities(display, Combined("Miasto", "template", "{Nazwa} ({Kraj})"));
        if (display == "pagination_table")
        {
            var pagedConnectionId = await CreateConnection(new
            {
                name = "Miasta stronicowane",
                method = "GET",
                url = "https://api.example.com/cities",
                queryParameters = new object[] { new { name = "pageNumber", required = false }, new { name = "pageSize", required = false } }
            });
            table["sources"]![0]!["connectionId"] = pagedConnectionId;
            table["pagination"] = new JsonObject { ["pageSize"] = 10 };
        }

        var id = await CreateTile(table);

        var preview = await Preview(id);

        Assert.That(
            preview["items"]!.AsArray()
                .Select(item => item!["fields"]!.AsArray().Single(field => field!["label"]!.GetValue<string>() == "Miasto")!["value"]?.GetValue<string>()),
            Is.EqualTo(new[] { "Kraków (PL)", "Praga (CZ)" }),
            preview.ToJsonString());
    }

    private async Task<Guid> CreateWeatherTile()
    {
        var weatherConnectionId = await CreateConnection(new
        {
            name = "Pogoda",
            method = "GET",
            url = "https://api.example.com/weather",
            queryParameters = new[] { new { name = "latlon", required = true } }
        });
        return await CreateTile(new JsonObject
        {
            ["name"] = "Pogoda",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "weather", ["kind"] = "connection", ["connectionId"] = weatherConnectionId }),
            ["fields"] = new JsonArray(new JsonObject { ["source"] = "weather", ["path"] = "$.temp", ["label"] = "Temperatura" })
        });
    }

    private JsonObject Panel(Guid addressId, Guid weatherTileId)
        => new()
        {
            ["name"] = "Panel",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "api", ["kind"] = "connection", ["connectionId"] = placeConnectionId }),
            ["fields"] = new JsonArray(new JsonObject { ["source"] = "api", ["path"] = "$.city", ["label"] = "Miasto" }),
            ["children"] = new JsonArray(
                new JsonObject { ["key"] = "address", ["tileId"] = addressId, ["position"] = 0 },
                new JsonObject { ["key"] = "weather", ["tileId"] = weatherTileId, ["position"] = 1 }),
            ["bindings"] = new JsonArray(new JsonObject
            {
                ["from"] = "address",
                ["to"] = "weather",
                ["map"] = new JsonArray(new JsonObject { ["field"] = "Współrzędne", ["parameter"] = "latlon" })
            })
        };

    private string LatLonOfWeatherRequest()
        => HttpUtility.ParseQueryString(new Uri(Factory.ExternalApi.Requests.Single(request => request.Url.Contains("/weather")).Url).Query)["latlon"];

    private JsonObject Address(params JsonObject[] fields)
        => new()
        {
            ["name"] = "Adres",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "api", ["kind"] = "connection", ["connectionId"] = placeConnectionId }),
            ["fields"] = new JsonArray(
            [
                PlaceField("$.street", "Ulica"),
                PlaceField("$.number", "Nr"),
                PlaceField("$.city", "Miasto"),
                PlaceField("$.lat", "Lat", Transformation("round", 2)),
                PlaceField("$.lon", "Lon", Transformation("round", 2)),
                .. fields
            ])
        };

    private JsonObject Cities(string display, params JsonObject[] fields)
        => new()
        {
            ["name"] = "Miasta",
            ["display"] = display,
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "cities", ["kind"] = "connection", ["connectionId"] = citiesConnectionId }),
            ["items"] = new JsonObject { ["source"] = "cities", ["path"] = "$.cities[*]", ["key"] = "ID" },
            ["fields"] = new JsonArray(
            [
                new JsonObject { ["source"] = "cities", ["path"] = "$.id", ["label"] = "ID" },
                new JsonObject { ["source"] = "cities", ["path"] = "$.name", ["label"] = "Nazwa", ["hidden"] = true },
                new JsonObject { ["source"] = "cities", ["path"] = "$.country", ["label"] = "Kraj", ["hidden"] = true },
                .. fields
            ])
        };

    private static JsonObject PlaceField(string path, string label, params object[] transformations)
        => new()
        {
            ["source"] = "api",
            ["path"] = path,
            ["label"] = label,
            ["hidden"] = true,
            ["transformations"] = JsonSerializer.SerializeToNode(transformations, JsonSerializerOptions.Web)
        };

    private static JsonObject Combined(
        string label,
        string key,
        string parameter = null,
        string[] fields = null,
        object[] transformations = null,
        bool hidden = false)
        => new()
        {
            ["label"] = label,
            ["combine"] = new JsonObject
            {
                ["key"] = key,
                ["parameter"] = parameter,
                ["fields"] = fields is null ? null : new JsonArray(fields.Select(field => (JsonNode)field).ToArray())
            },
            ["hidden"] = hidden,
            ["transformations"] = JsonSerializer.SerializeToNode(transformations ?? [], JsonSerializerOptions.Web)
        };

    private static JsonNode Field(JsonObject preview, string label)
        => preview["fields"]!.AsArray().Single(field => field!["label"]!.GetValue<string>() == label);

    private static string Value(JsonObject preview, string label) => Field(preview, label)["value"]?.GetValue<string>();

    private async Task<JsonObject> Preview(Guid id, object values = null)
    {
        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", values ?? new { });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonObject>();
    }
}
