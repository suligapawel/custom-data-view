using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Application.Dictionaries;
using CustomDataView.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CustomDataView.WebApi.Tests.Tiles;

public class MapperTransformationTests : TileApiTests
{
    private const string Dir = "scenario_TEMP-045";

    private const string StatusesUrl = "https://api.example.com/statuses";

    private const string StatusesResponse =
        """
        [{"key": "NEW", "translation": "Nowe", "weight": 1}, {"key": "DONE", "translation": "Zrealizowane", "weight": 3}]
        """;

    private const string OrderResponse =
        """
        {"status": "NEW", "statuses": ["NEW", "DONE"], "level": 6, "note": null}
        """;

    private Guid statusesConnectionId;
    private Guid ordersConnectionId;
    private string levelsResponse;
    private string orderResponse;
    private bool statusesAvailable;
    private string statusesResponse;

    [SetUp]
    public async Task CreateDictionaryConnections()
    {
        levelsResponse = "[]";
        orderResponse = OrderResponse;
        statusesAvailable = true;
        statusesResponse = StatusesResponse;
        Factory.ExternalApi.Respond = (request, _) => Task.FromResult(request.RequestUri!.AbsolutePath switch
        {
            "/statuses" when statusesAvailable => Json(statusesResponse),
            "/orders/1" => Json(orderResponse),
            "/levels" => Json(levelsResponse),
            "/categories" => Json("""{"name": "Sprzedaż"}"""),
            "/status-details" => Json("""{"description": "Szczegóły"}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        statusesConnectionId = await CreateConnection(new { name = "Statusy", method = "GET", url = StatusesUrl });
        ordersConnectionId = await CreateConnection(new { name = "Zamówienia", method = "GET", url = "https://api.example.com/orders/1" });
    }

    [Test(Description = $"{Dir} - #2")]
    public async Task Given_FieldMappedByLoadedDictionary_When_UserPreviewsTile_Then_FieldHoldsDictionaryValueWithoutCallingDictionarySource()
    {
        var statusesId = await CreateTile(Statuses());
        var orderId = await CreateTile(Order(OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie"))));
        await LoadDictionariesOnStartup();
        var requestsBeforePreview = StatusRequests();

        var preview = await Preview(orderId);

        Assert.That(Value(preview, "Status"), Is.EqualTo("Nowe"), preview.ToJsonString());
        Assert.That(StatusRequests(), Is.EqualTo(requestsBeforePreview));
    }

    [Test(Description = $"{Dir} - #3")]
    public async Task Given_DictionaryWithNumberValue_When_UserPreviewsTile_Then_MappedValueStaysNumber()
    {
        levelsResponse = """[{"from": 6, "to": 8}]""";
        var levelsId = await CreateTile(await Levels());
        var orderId = await CreateTile(Order(
            OrderField("$.level", "Poziom", Mapper(levelsId, "Na"), Transformation("suffix", " pkt")),
            OrderField("$.level", "Poziom liczbowy", Mapper(levelsId, "Na"))));
        await LoadDictionariesOnStartup();

        var preview = await Preview(orderId);

        Assert.That(Value(preview, "Poziom"), Is.EqualTo("8 pkt"), preview.ToJsonString());
        Assert.That(JsonNode.DeepEquals(Field(preview, "Poziom liczbowy")["value"], JsonValue.Create(8)), Is.True, preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #4")]
    public async Task Given_DictionaryKeyWrittenAsText_When_UserPreviewsTileWithNumberValue_Then_NumberFindsTextKey()
    {
        levelsResponse = """[{"from": "6", "to": "wysoki"}]""";
        var levelsId = await CreateTile(await Levels());
        var orderId = await CreateTile(Order(OrderField("$.level", "Poziom", Mapper(levelsId, "Na"))));
        await LoadDictionariesOnStartup();

        var preview = await Preview(orderId);

        Assert.That(Value(preview, "Poziom"), Is.EqualTo("wysoki"), preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #5")]
    public async Task Given_DictionaryFieldsWithTransformationAndCombination_When_UserPreviewsTile_Then_DictionaryHoldsTransformedValues()
    {
        var statuses = Statuses(new JsonObject
        {
            ["label"] = "Opis",
            ["combine"] = new JsonObject { ["key"] = "template", ["parameter"] = "{Tłumaczenie} ({Waga})" }
        });
        statuses["fields"]![1]!["transformations"] = new JsonArray(new JsonObject { ["key"] = "uppercase" });
        var statusesId = await CreateTile(statuses);
        var orderId = await CreateTile(Order(
            OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie")),
            OrderField("$.status", "Opis statusu", Mapper(statusesId, "Opis"))));
        await LoadDictionariesOnStartup();

        var preview = await Preview(orderId);

        Assert.That(Value(preview, "Status"), Is.EqualTo("NOWE"), preview.ToJsonString());
        Assert.That(Value(preview, "Opis statusu"), Is.EqualTo("NOWE (1)"));
    }

    [Test(Description = $"{Dir} - #6")]
    public async Task Given_ListFieldMappedByDictionary_When_UserPreviewsTile_Then_EachElementIsMapped()
    {
        var statusesId = await CreateTile(Statuses());
        var orderId = await CreateTile(Order(OrderField("$.statuses[*]", "Historia", Mapper(statusesId, "Tłumaczenie"))));
        await LoadDictionariesOnStartup();

        var preview = await Preview(orderId);

        Assert.That(
            JsonNode.DeepEquals(Field(preview, "Historia")["value"], JsonNode.Parse("""["Nowe", "Zrealizowane"]""")),
            Is.True,
            preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #7")]
    public async Task Given_ValueMissingFromDictionary_When_UserPreviewsTile_Then_FieldHasMissingKeyErrorAndOtherFieldsHaveValues()
    {
        orderResponse = """{"status": "LOST", "level": 6}""";
        var statusesId = await CreateTile(Statuses());
        var infoId = await CreateStatusInfoTile();
        var order = Order(
            OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie")),
            OrderField("$.level", "Poziom"));
        order["children"] = new JsonArray(new JsonObject { ["key"] = "info", ["tileId"] = infoId, ["position"] = 0 });
        order["bindings"] = new JsonArray(new JsonObject
        {
            ["from"] = "parent",
            ["to"] = "info",
            ["map"] = new JsonArray(new JsonObject { ["field"] = "Status", ["parameter"] = "status" })
        });
        var orderId = await CreateTile(order);
        await LoadDictionariesOnStartup();

        var preview = await Preview(orderId);

        Assert.That(
            JsonNode.DeepEquals(
                Field(preview, "Status"),
                JsonNode.Parse("""{"label": "Status", "error": {"code": "transformation_failed", "transformation": "mapper", "reason": "missing_dictionary_key"}}""")),
            Is.True,
            preview.ToJsonString());
        var info = preview["children"]![0]!;
        Assert.That(info["state"]!.GetValue<string>(), Is.EqualTo("error"));
        Assert.That(info["error"]!["code"]!.GetValue<string>(), Is.EqualTo("dependency_failed"));
        Assert.That(info["error"]!["cause"]!["field"]!.GetValue<string>(), Is.EqualTo("Status"));
        Assert.That(Field(preview, "Poziom")["value"]!.GetValue<int>(), Is.EqualTo(6));
    }

    [Test(Description = $"{Dir} - #8")]
    public async Task Given_MissingInputValue_When_UserPreviewsTile_Then_FieldHasNeitherValueNorError()
    {
        var statusesId = await CreateTile(Statuses());
        var orderId = await CreateTile(Order(OrderField("$.note", "Notatka", Mapper(statusesId, "Tłumaczenie"))));
        await LoadDictionariesOnStartup();

        var preview = await Preview(orderId);

        Assert.That(
            JsonNode.DeepEquals(Field(preview, "Notatka"), JsonNode.Parse("""{"label": "Notatka", "value": null}""")),
            Is.True,
            preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #9")]
    public async Task Given_DictionarySourceDownOnStartup_When_TileIsSavedAgain_Then_DictionaryBecomesAvailable()
    {
        levelsResponse = """[{"from": 6, "to": 8}]""";
        var statusesId = await CreateTile(Statuses());
        var levelsId = await CreateTile(await Levels());
        var orderId = await CreateTile(Order(
            OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie")),
            OrderField("$.level", "Poziom", Mapper(levelsId, "Na"))));
        statusesAvailable = false;
        await LoadDictionariesOnStartup();

        var preview = await Preview(orderId);

        Assert.That(
            JsonNode.DeepEquals(
                Field(preview, "Status"),
                JsonNode.Parse("""{"label": "Status", "error": {"code": "transformation_failed", "transformation": "mapper", "reason": "dictionary_unavailable"}}""")),
            Is.True,
            preview.ToJsonString());
        Assert.That(Field(preview, "Poziom")["value"]!.GetValue<int>(), Is.EqualTo(8));

        statusesAvailable = true;
        await UpdateTile(statusesId, Statuses());
        await Factory.DictionaryLoads.ProcessPending();
        var previewAfterSave = await Preview(orderId);

        Assert.That(Value(previewAfterSave, "Status"), Is.EqualTo("Nowe"), previewAfterSave.ToJsonString());
    }

    [TestCase("""[{"key": "NEW", "translation": "Nowe"}, {"key": "NEW", "translation": "Inne"}]""", null)]
    [TestCase("""[{"key": "NEW", "translation": "Nowe"}, {"translation": "Bez klucza"}]""", null)]
    [TestCase("""[{"key": "NEW"}]""", null)]
    [TestCase("""[{"key": null, "translation": "Pusty"}, {"key": null, "translation": "Inny"}, {"key": "NEW", "translation": "Nowe"}]""", "Nowe")]
    [Test(Description = $"{Dir} - #10")]
    public async Task Given_InvalidDictionaryData_When_UserPreviewsTile_Then_DictionaryIsNotLoaded(string data, string expectedStatus)
    {
        statusesResponse = data;
        var statusesId = await CreateTile(Statuses());
        var orderId = await CreateTile(Order(OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie"))));
        await LoadDictionariesOnStartup();

        var preview = await Preview(orderId);

        Assert.That(
            Field(preview, "Status")["error"]?["reason"]?.GetValue<string>() ?? Value(preview, "Status"),
            Is.EqualTo(expectedStatus ?? "dictionary_unavailable"),
            preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #11")]
    public async Task Given_NoTileUsedDictionaryOnStartup_When_UserSavesTileWithMapper_Then_DictionaryIsLoadedInBackground()
    {
        var statusesId = await CreateTile(Statuses());
        await LoadDictionariesOnStartup();

        var orderId = await CreateTile(Order(OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie"))));

        Assert.That(StatusRequests(), Is.Zero);
        await Factory.DictionaryLoads.ProcessPending();
        var preview = await Preview(orderId);
        Assert.That(Value(preview, "Status"), Is.EqualTo("Nowe"), preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #11")]
    public async Task Given_TileWithoutMapper_When_UserAddsMapperByEditingTile_Then_DictionaryIsLoadedInBackground()
    {
        var statusesId = await CreateTile(Statuses());
        var orderId = await CreateTile(Order(OrderField("$.status", "Status")));
        await LoadDictionariesOnStartup();

        await UpdateTile(orderId, Order(OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie"))));

        await Factory.DictionaryLoads.ProcessPending();
        var preview = await Preview(orderId);
        Assert.That(Value(preview, "Status"), Is.EqualTo("Nowe"), preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #12")]
    public async Task Given_LoadedDictionary_When_DictionaryTileIsSaved_Then_PreviousVersionWorksUntilReloadAndFailedReloadMakesItUnavailable()
    {
        var statusesId = await CreateTile(Statuses());
        var orderId = await CreateTile(Order(OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie"))));
        await LoadDictionariesOnStartup();
        var uppercaseStatuses = Statuses();
        uppercaseStatuses["fields"]![1]!["transformations"] = new JsonArray(new JsonObject { ["key"] = "uppercase" });

        await UpdateTile(statusesId, uppercaseStatuses);

        Assert.That(Value(await Preview(orderId), "Status"), Is.EqualTo("Nowe"));
        await Factory.DictionaryLoads.ProcessPending();
        Assert.That(Value(await Preview(orderId), "Status"), Is.EqualTo("NOWE"));

        statusesAvailable = false;
        await UpdateTile(statusesId, uppercaseStatuses);
        await Factory.DictionaryLoads.ProcessPending();

        var preview = await Preview(orderId);
        Assert.That(Field(preview, "Status")["error"]?["reason"]?.GetValue<string>(), Is.EqualTo("dictionary_unavailable"), preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #13")]
    public async Task Given_DictionaryTileWithOtherSourcesAndChildren_When_DictionariesAreLoaded_Then_OnlyItemsSourceIsCalledWithDefaultParameters()
    {
        var statusesWithLanguageId = await CreateConnection(new
        {
            name = "Statusy",
            method = "GET",
            url = StatusesUrl,
            queryParameters = new[] { new { name = "lang", required = false, defaultValue = "pl" } }
        });
        var categoriesId = await CreateConnection(new { name = "Kategorie", method = "GET", url = "https://api.example.com/categories" });
        var detailsConnectionId = await CreateConnection(new { name = "Szczegóły", method = "GET", url = "https://api.example.com/status-details" });
        var detailsId = await CreateTile(new JsonObject
        {
            ["name"] = "Szczegóły statusu",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "details", ["kind"] = "connection", ["connectionId"] = detailsConnectionId }),
            ["fields"] = new JsonArray(SourceField("details", "$.description", "Opis", []))
        });
        var category = SourceField("categories", "$.name", "Kategoria", []);
        category["hidden"] = true;
        var statuses = Statuses(category);
        statuses["sources"]![0]!["connectionId"] = statusesWithLanguageId;
        statuses["sources"]!.AsArray().Add(new JsonObject { ["key"] = "categories", ["kind"] = "connection", ["connectionId"] = categoriesId });
        statuses["children"] = new JsonArray(new JsonObject { ["key"] = "details", ["tileId"] = detailsId, ["position"] = 0 });
        var statusesId = await CreateTile(statuses);
        await CreateTile(Order(OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie"))));
        var requestsBeforeLoading = Factory.ExternalApi.Requests.Count;

        await LoadDictionariesOnStartup();

        Assert.That(
            Factory.ExternalApi.Requests.Skip(requestsBeforeLoading).Select(request => request.Url),
            Is.EqualTo(new[] { $"{StatusesUrl}?lang=pl" }));
    }

    [TestCase("text")]
    [TestCase("missing-tile")]
    [TestCase("self")]
    [TestCase("no-items")]
    [TestCase("missing-label")]
    [TestCase("required-parameter")]
    [TestCase("required-input")]
    [TestCase("pagination")]
    [TestCase("dictionary-with-mapper")]
    [Test(Description = $"{Dir} - #14")]
    public async Task Given_InvalidDictionaryReference_When_UserSavesTile_Then_SaveIsRejected(string problem)
    {
        var statusesId = await CreateTile(Statuses());
        var dictionaryId = problem switch
        {
            "no-items" => await CreateTile(Order(OrderField("$.status", "Tłumaczenie"))),
            "required-parameter" => await CreateTile(await StatusesWithLanguage(required: true)),
            "required-input" => await CreateTile(StatusesFromInput()),
            "pagination" => await CreateTile(await PagedStatuses()),
            "dictionary-with-mapper" => await CreateTile(Statuses(StatusField("$.key", "Opis", Mapper(await CreateTile(await Levels()), "Na")))),
            _ => statusesId
        };
        var parameter = problem switch
        {
            "text" => "Statusy zamówień",
            "missing-tile" => $"{Guid.NewGuid()}:Tłumaczenie",
            "missing-label" => $"{dictionaryId}:Kolor",
            _ => $"{dictionaryId}:Tłumaczenie"
        };

        var response = problem == "self"
            ? await Client.PutAsJsonAsync($"{TilesUrl}/{statusesId}", Statuses(StatusField("$.key", "Opis", Transformation("mapper", parameter))))
            : await Client.PostAsJsonAsync(TilesUrl, Order(OrderField("$.status", "Status", Transformation("mapper", parameter))));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var errors = (await response.Content.ReadFromJsonAsync<JsonObject>())!["errors"]!.AsObject();
        Assert.That(
            errors.Select(error => error.Key),
            Is.EqualTo(new[] { $"fields[{(problem == "self" ? 3 : 0)}].transformations[0].parameter" }),
            errors.ToJsonString());
    }

    [TestCase("remove-value-field")]
    [TestCase("rename-value-field")]
    [TestCase("details-without-items")]
    [TestCase("pagination")]
    [TestCase("mapper")]
    [TestCase("required-parameter-source")]
    [TestCase("input-source")]
    [TestCase("change-items-key")]
    [Test(Description = $"{Dir} - #15")]
    public async Task Given_DictionaryUsedByTile_When_UserBreaksDictionaryTile_Then_EditIsBlocked(string change)
    {
        var statusesId = await CreateTile(Statuses());
        var orderId = await CreateTile(Order(OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie"))));
        var statuses = change == "pagination" ? await PagedStatuses() : Statuses();
        switch (change)
        {
            case "rename-value-field":
                statuses["fields"]![1]!["label"] = "Nazwa";
                break;
            case "details-without-items":
                statuses["display"] = "details";
                statuses.Remove("items");
                break;
            case "mapper":
                statuses["fields"]![2]!["transformations"] = new JsonArray(
                    JsonSerializer.SerializeToNode(Mapper(await CreateTile(await Levels()), "Na"), JsonSerializerOptions.Web));
                break;
            case "required-parameter-source":
                var languageSource = await StatusesWithLanguage(required: true);
                languageSource["sources"]![0]!["key"] = "language";
                statuses["sources"]!.AsArray().Add(languageSource["sources"]![0]!.DeepClone());
                break;
            case "input-source":
                statuses["sources"]!.AsArray().Add(new JsonObject { ["key"] = "extra", ["kind"] = "input" });
                break;
            case "change-items-key":
                statuses["items"]!["key"] = "Waga";
                break;
            case "pagination":
                break;
            default:
                statuses["fields"]!.AsArray().RemoveAt(1);
                break;
        }

        var response = await Client.PutAsJsonAsync($"{TilesUrl}/{statusesId}", statuses);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(
            JsonNode.DeepEquals(
                await response.Content.ReadFromJsonAsync<JsonObject>(),
                JsonNode.Parse($$"""{"code": "dictionary_in_use", "fields": ["Tłumaczenie"], "tiles": [{"id": "{{orderId}}", "name": "Zamówienie"}]}""")),
            Is.True);
    }

    [Test(Description = $"{Dir} - #15")]
    public async Task Given_DictionaryUsedByTile_When_UserRenamesOtherField_Then_EditIsSaved()
    {
        var statusesId = await CreateTile(Statuses());
        await CreateTile(Order(OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie"))));
        var statuses = Statuses();
        statuses["fields"]![2]!["label"] = "Kolejność";

        var response = await Client.PutAsJsonAsync($"{TilesUrl}/{statusesId}", statuses);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test(Description = $"{Dir} - #16")]
    public async Task Given_DictionaryUsedByTile_When_UserDeletesDictionaryTile_Then_DeleteIsBlocked()
    {
        var statusesId = await CreateTile(Statuses());
        var orderId = await CreateTile(Order(OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie"))));

        var response = await Client.DeleteAsync($"{TilesUrl}/{statusesId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(
            JsonNode.DeepEquals(
                await response.Content.ReadFromJsonAsync<JsonObject>(),
                JsonNode.Parse($$"""{"code": "tile_in_use", "tiles": [{"id": "{{orderId}}", "name": "Zamówienie"}]}""")),
            Is.True);
    }

    [Test(Description = $"{Dir} - #17")]
    public async Task Given_DictionaryUsedByTile_When_UserPreviewsDictionaryTile_Then_ItShowsItemsFromItsConnection()
    {
        var statusesId = await CreateTile(Statuses());
        await CreateTile(Order(OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie"))));

        var preview = await Preview(statusesId);

        Assert.That(StatusRequests(), Is.EqualTo(1));
        Assert.That(
            preview["items"]!.AsArray().Select(item => (
                Key: item!["fields"]![0]!["value"]!.GetValue<string>(),
                Translation: item["fields"]![1]!["value"]!.GetValue<string>())),
            Is.EqualTo(new[] { ("NEW", "Nowe"), ("DONE", "Zrealizowane") }),
            preview.ToJsonString());
    }

    [Test(Description = $"{Dir} - #18")]
    public async Task Given_OneLoadedDictionary_When_UserPreviewsUnsavedTile_Then_OnlyLoadedDictionaryIsUsedAndNothingIsLoaded()
    {
        var statusesId = await CreateTile(Statuses());
        await CreateTile(Order(OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie"))));
        await LoadDictionariesOnStartup();
        var draft = Order(
            OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie")),
            OrderField("$.status", "Waga statusu", Mapper(statusesId, "Waga")));

        var response = await Client.PostAsJsonAsync($"{TilesUrl}/preview", new { tile = draft });

        response.EnsureSuccessStatusCode();
        var preview = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(Value(preview, "Status"), Is.EqualTo("Nowe"), preview.ToJsonString());
        Assert.That(Field(preview, "Waga statusu")["error"]?["reason"]?.GetValue<string>(), Is.EqualTo("dictionary_unavailable"));
        await Factory.DictionaryLoads.ProcessPending();
        var previewAfterLoading = await (await Client.PostAsJsonAsync($"{TilesUrl}/preview", new { tile = draft })).Content.ReadFromJsonAsync<JsonObject>();
        Assert.That(Field(previewAfterLoading, "Waga statusu")["error"]?["reason"]?.GetValue<string>(), Is.EqualTo("dictionary_unavailable"));
    }

    [Test(Description = $"{Dir} - #18")]
    public async Task Given_MapperParameterInWrongFormat_When_UserPreviewsUnsavedTile_Then_PreviewIsRejected()
    {
        var draft = Order(OrderField("$.status", "Status", Transformation("mapper", "Statusy zamówień")));

        var response = await Client.PostAsJsonAsync($"{TilesUrl}/preview", new { tile = draft });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var errors = (await response.Content.ReadFromJsonAsync<JsonObject>())!["errors"]!.AsObject();
        Assert.That(errors.Select(error => error.Key), Is.EqualTo(new[] { "tile.fields[0].transformations[0].parameter" }), errors.ToJsonString());
    }

    [Test(Description = $"{Dir} - #19")]
    public async Task Given_DictionarySourceNotResponding_When_ApplicationStarts_Then_RequestsAreServedBeforeDictionariesLoad()
    {
        var statusesId = await CreateTile(Statuses());
        var orderId = await CreateTile(Order(OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie"))));
        var statusesResponds = new TaskCompletionSource();
        var respond = Factory.ExternalApi.Respond;
        Factory.ExternalApi.Respond = async (request, cancellationToken) =>
        {
            if (request.RequestUri!.AbsolutePath == "/statuses")
            {
                await statusesResponds.Task.WaitAsync(cancellationToken);
            }

            return await respond(request, cancellationToken);
        };
        using var service = new DictionaryLoadingService(
            Factory.Services.GetRequiredService<DictionaryLoader>(),
            new DictionaryLoadQueue(),
            NullLogger<DictionaryLoadingService>.Instance);
        using var stopping = new CancellationTokenSource();

        var starting = service.StartAsync(stopping.Token);

        Assert.That(starting.IsCompleted, Is.True);
        var preview = await Preview(orderId);
        Assert.That(Field(preview, "Status")["error"]?["reason"]?.GetValue<string>(), Is.EqualTo("dictionary_unavailable"), preview.ToJsonString());
        statusesResponds.SetResult();
        Assert.That(await StatusAfterBackgroundLoading(orderId), Is.EqualTo("Nowe"));
        await stopping.CancelAsync();
        await service.StopAsync(CancellationToken.None);
    }

    [Test(Description = $"{Dir} - #11")]
    public async Task Given_RunningApplication_When_DictionaryLoadIsRequested_Then_BackgroundServiceLoadsItDespiteEarlierFailure()
    {
        var statusesId = await CreateTile(Statuses());
        var orderId = await CreateTile(Order(OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie"))));
        var queue = new DictionaryLoadQueue();
        using var service = new DictionaryLoadingService(
            Factory.Services.GetRequiredService<DictionaryLoader>(),
            queue,
            NullLogger<DictionaryLoadingService>.Instance);
        using var stopping = new CancellationTokenSource();
        await service.StartAsync(stopping.Token);
        Assert.That(await StatusAfterBackgroundLoading(orderId), Is.EqualTo("Nowe"));
        Factory.Services.GetRequiredService<MapperDictionaries>().Clear();

        queue.Enqueue([new DictionaryReference(Guid.NewGuid(), "Tłumaczenie")]);
        queue.Enqueue([new DictionaryReference(statusesId, "Tłumaczenie")]);

        Assert.That(await StatusAfterBackgroundLoading(orderId), Is.EqualTo("Nowe"));
        await stopping.CancelAsync();
        await service.StopAsync(CancellationToken.None);
    }

    [Test(Description = $"{Dir} - #19")]
    public async Task Given_TilesUnavailableOnStartup_When_ApplicationStarts_Then_BackgroundServiceKeepsServingLoadRequests()
    {
        var statusesId = await CreateTile(Statuses());
        var orderId = await CreateTile(Order(OrderField("$.status", "Status", Mapper(statusesId, "Tłumaczenie"))));
        var queue = new DictionaryLoadQueue();
        using var service = new DictionaryLoadingService(
            Factory.Services.GetRequiredService<DictionaryLoader>(),
            queue,
            NullLogger<DictionaryLoadingService>.Instance);
        using var stopping = new CancellationTokenSource();
        Factory.Services.GetRequiredService<TestTileRepository>().FailsToListTiles = true;

        await service.StartAsync(stopping.Token);
        queue.Enqueue([new DictionaryReference(statusesId, "Tłumaczenie")]);

        Assert.That(await StatusAfterBackgroundLoading(orderId), Is.EqualTo("Nowe"));
        await stopping.CancelAsync();
        await service.StopAsync(CancellationToken.None);
    }

    private async Task<string> StatusAfterBackgroundLoading(Guid orderId)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (Value(await Preview(orderId), "Status") is { } status)
            {
                return status;
            }

            await Task.Delay(100);
        }

        return null;
    }

    private Task LoadDictionariesOnStartup()
        => Factory.Services.GetRequiredService<DictionaryLoader>().LoadAll(CancellationToken.None);

    private async Task UpdateTile(Guid id, JsonObject tile)
    {
        var response = await Client.PutAsJsonAsync($"{TilesUrl}/{id}", tile);
        response.EnsureSuccessStatusCode();
    }

    private int StatusRequests() => Factory.ExternalApi.Requests.Count(request => request.Url == StatusesUrl);

    private JsonObject Statuses(params JsonObject[] fields)
        => new()
        {
            ["name"] = "Statusy zamówień",
            ["display"] = "table",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "statuses", ["kind"] = "connection", ["connectionId"] = statusesConnectionId }),
            ["items"] = new JsonObject { ["source"] = "statuses", ["path"] = "$[*]", ["key"] = "Klucz" },
            ["fields"] = new JsonArray(
            [
                StatusField("$.key", "Klucz"),
                StatusField("$.translation", "Tłumaczenie"),
                StatusField("$.weight", "Waga"),
                .. fields
            ])
        };

    private async Task<JsonObject> StatusesWithLanguage(bool required)
    {
        var connectionId = await CreateConnection(new
        {
            name = "Statusy w języku",
            method = "GET",
            url = StatusesUrl,
            queryParameters = new[] { new { name = "lang", required } }
        });
        var statuses = Statuses();
        statuses["sources"]![0]!["connectionId"] = connectionId;
        return statuses;
    }

    private async Task<JsonObject> PagedStatuses()
    {
        var connectionId = await CreateConnection(new
        {
            name = "Statusy stronicowane",
            method = "GET",
            url = StatusesUrl,
            queryParameters = new[] { new { name = "pageNumber", required = false }, new { name = "pageSize", required = false } }
        });
        var statuses = Statuses();
        statuses["display"] = "pagination_table";
        statuses["sources"]![0]!["connectionId"] = connectionId;
        statuses["pagination"] = new JsonObject { ["pageSize"] = 10 };
        return statuses;
    }

    private JsonObject StatusesFromInput()
    {
        var statuses = Statuses();
        statuses["sources"] = new JsonArray(new JsonObject { ["key"] = "statuses", ["kind"] = "input" });
        return statuses;
    }

    private async Task<Guid> CreateStatusInfoTile()
    {
        var infoConnectionId = await CreateConnection(new
        {
            name = "Opis statusu",
            method = "GET",
            url = "https://api.example.com/status-info",
            queryParameters = new[] { new { name = "status", required = true } }
        });
        return await CreateTile(new JsonObject
        {
            ["name"] = "Opis statusu",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "info", ["kind"] = "connection", ["connectionId"] = infoConnectionId }),
            ["fields"] = new JsonArray(SourceField("info", "$.description", "Opis", []))
        });
    }

    private async Task<JsonObject> Levels()
    {
        var levelsConnectionId = await CreateConnection(new { name = "Poziomy", method = "GET", url = "https://api.example.com/levels" });
        return new JsonObject
        {
            ["name"] = "Mapa poziomów",
            ["display"] = "table",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "levels", ["kind"] = "connection", ["connectionId"] = levelsConnectionId }),
            ["items"] = new JsonObject { ["source"] = "levels", ["path"] = "$[*]", ["key"] = "Z" },
            ["fields"] = new JsonArray(SourceField("levels", "$.from", "Z", []), SourceField("levels", "$.to", "Na", []))
        };
    }

    private JsonObject Order(params JsonObject[] fields)
        => new()
        {
            ["name"] = "Zamówienie",
            ["display"] = "details",
            ["sources"] = new JsonArray(new JsonObject { ["key"] = "orders", ["kind"] = "connection", ["connectionId"] = ordersConnectionId }),
            ["fields"] = new JsonArray(fields.Select(field => (JsonNode)field).ToArray())
        };

    private static JsonObject StatusField(string path, string label, params object[] transformations)
        => SourceField("statuses", path, label, transformations);

    private static JsonObject OrderField(string path, string label, params object[] transformations)
        => SourceField("orders", path, label, transformations);

    private static JsonObject SourceField(string source, string path, string label, object[] transformations)
        => new()
        {
            ["source"] = source,
            ["path"] = path,
            ["label"] = label,
            ["transformations"] = JsonSerializer.SerializeToNode(transformations, JsonSerializerOptions.Web)
        };

    private static object Mapper(Guid dictionaryTileId, string valueLabel) => Transformation("mapper", $"{dictionaryTileId}:{valueLabel}");

    private static JsonNode Field(JsonObject preview, string label)
        => preview["fields"]!.AsArray().Single(field => field!["label"]!.GetValue<string>() == label);

    private static string Value(JsonObject preview, string label) => Field(preview, label)["value"]?.GetValue<string>();

    private async Task<JsonObject> Preview(Guid id)
    {
        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", new { });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonObject>();
    }
}
