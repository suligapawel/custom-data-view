using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Exceptions;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Tests.Tiles;

public class DictionaryTileTests
{
    private const string Dir = "scenario_TEMP-045";

    private static readonly Guid ConnectionId = Guid.NewGuid();
    private static readonly Guid OtherDictionaryId = Guid.NewGuid();
    private static readonly TileContract OpenContract = new([new TileContractParameter("lang", false)], [], new TileContractOutputs([], true));

    [Test(Description = $"{Dir} - #14")]
    public void Given_TileWithItemsAndValueField_When_ItIsCheckedAsDictionary_Then_ItHasNoViolations()
    {
        Assert.That(Statuses().DictionaryViolations("Tłumaczenie", OpenContract), Is.Empty);
        Assert.That(Statuses().DictionaryViolations(" tłumaczenie ", OpenContract), Is.Empty);
    }

    [TestCase("no-items")]
    [TestCase("missing-label")]
    [TestCase("non-item-field")]
    [TestCase("required-parameter")]
    [TestCase("input")]
    [TestCase("pagination")]
    [TestCase("mapper")]
    [Test(Description = $"{Dir} - #14")]
    public void Given_TileBreakingDictionaryRule_When_ItIsCheckedAsDictionary_Then_ItHasViolation(string problem)
    {
        var statuses = Statuses();
        var label = "Tłumaczenie";
        var contract = OpenContract;
        switch (problem)
        {
            case "no-items":
                statuses = statuses with { Items = null };
                break;
            case "missing-label":
                label = "Kolor";
                break;
            case "non-item-field":
                statuses = statuses with
                {
                    Sources = [.. statuses.Sources, new TileSource("categories", TileSourceKinds.Connection, ConnectionId)],
                    Fields = [.. statuses.Fields, new TileField("categories", "$.name", "Kategoria", [], Hidden: true)]
                };
                label = "Kategoria";
                break;
            case "required-parameter":
                contract = contract with { Parameters = [new TileContractParameter("lang", true)] };
                break;
            case "input":
                contract = contract with { Inputs = ["extra"] };
                break;
            case "pagination":
                statuses = statuses with { Pagination = new TilePagination("pageNumber", "pageSize", 10, null) };
                break;
            default:
                statuses = statuses with
                {
                    Fields = [.. statuses.Fields, new TileField("statuses", "$.weight", "Waga", [new TransformationStep("mapper", $"{OtherDictionaryId}:Na")])]
                };
                break;
        }

        Assert.That(statuses.DictionaryViolations(label, contract), Has.Count.EqualTo(1));
    }

    [Test(Description = $"{Dir} - #13")]
    public void Given_TileWithOtherSourcesChildrenAndPagination_When_ItIsPreparedForLoading_Then_OnlyItemsSourceAndItemFieldsRemain()
    {
        var statuses = Statuses() with
        {
            Sources = [.. Statuses().Sources, new TileSource("categories", TileSourceKinds.Connection, ConnectionId)],
            Fields = [.. Statuses().Fields, new TileField("categories", "$.name", "Kategoria", [], Hidden: true)],
            Children = [new TileChild("details", Guid.NewGuid(), 0, null, TileChildScopes.Tile, TileChildLoads.Eager)]
        };

        var source = statuses.AsDictionarySource();

        Assert.That(source.Sources.Select(tileSource => tileSource.Key), Is.EqualTo(new[] { "statuses" }));
        Assert.That(source.Fields.Select(field => field.Label), Is.EqualTo(new[] { "Klucz", "Tłumaczenie" }));
        Assert.That(source.Children, Is.Empty);
    }

    [Test(Description = $"{Dir} - #11")]
    public void Given_FieldsWithMapperSteps_When_ReferencesAreRead_Then_ReadableReferencesAreReturned()
    {
        var dictionaryId = Guid.NewGuid();
        var orders = Statuses() with
        {
            Fields =
            [
                new TileField("statuses", "$.status", "Status", [new TransformationStep("uppercase"), new TransformationStep("mapper", $"{dictionaryId}:Tłumaczenie")]),
                new TileField("statuses", "$.level", "Poziom", [new TransformationStep("mapper", "Poziomy")])
            ]
        };

        Assert.That(orders.DictionaryReferences(), Is.EqualTo(new[] { new DictionaryReference(dictionaryId, "Tłumaczenie") }));
    }

    [TestCase("rename-value-field")]
    [TestCase("change-items-key")]
    [Test(Description = $"{Dir} - #15")]
    public void Given_DictionaryUsedByTile_When_EditBreaksIt_Then_EditIsBlockedWithFieldsAndTiles(string change)
    {
        var tile = Tile.Restore(Guid.NewGuid(), Statuses());
        var orders = new TileReference(Guid.NewGuid(), "Zamówienia");
        var replacement = change == "rename-value-field"
            ? Statuses() with { Fields = [Statuses().Fields[0], Statuses().Fields[1] with { Label = "Nazwa" }] }
            : Statuses() with { Items = new TileItems("statuses", "$[*]", "Tłumaczenie") };

        var exception = Assert.Throws<DictionaryInUseException>(() =>
            tile.EnsureDictionariesKept(replacement, [new TileDictionaryUse(orders, "Tłumaczenie")], OpenContract));

        Assert.That(exception.Fields, Is.EqualTo(new[] { "Tłumaczenie" }));
        Assert.That(exception.Tiles, Is.EqualTo(new[] { orders }));
    }

    [Test(Description = $"{Dir} - #15")]
    public void Given_DictionaryUsedByTile_When_EditKeepsIt_Then_EditIsAllowed()
    {
        var tile = Tile.Restore(Guid.NewGuid(), Statuses());
        var replacement = Statuses() with { Name = "Statusy" };

        Assert.DoesNotThrow(() =>
            tile.EnsureDictionariesKept(replacement, [new TileDictionaryUse(new TileReference(Guid.NewGuid(), "Zamówienia"), "Tłumaczenie")], OpenContract));
    }

    private static TileDefinition Statuses()
        => new(
            "Statusy zamówień",
            TileDisplays.Table,
            [new TileSource("statuses", TileSourceKinds.Connection, ConnectionId)],
            new TileItems("statuses", "$[*]", "Klucz"),
            [new TileField("statuses", "$.key", "Klucz", []), new TileField("statuses", "$.translation", "Tłumaczenie", [])]);
}
