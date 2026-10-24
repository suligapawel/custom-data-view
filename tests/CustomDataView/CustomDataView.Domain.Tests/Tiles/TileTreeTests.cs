using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Services;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Tests.Tiles;

public class TileTreeTests
{
    private readonly Dictionary<Guid, TileDefinition> tiles = [];

    [Test(Description = "scenario_TEMP-036 - #6")]
    public void Given_ShippingEmbedsCarrier_When_CarrierIsCheckedForEmbeddingShipping_Then_ShippingEmbedsCarrier()
    {
        var carrier = Add("Przewoźnik");
        var shipping = Add("Dostawa", Child("carrier", carrier));

        Assert.That(Tree().Embeds(tiles[shipping], carrier), Is.True);
        Assert.That(Tree().Embeds(tiles[carrier], shipping), Is.False);
    }

    [Test(Description = "scenario_TEMP-041-043 - #7")]
    public void Given_ChainOfFourLevels_When_TopIsCheckedForBottom_Then_DeepEmbeddingIsFound()
    {
        var bottom = Add("Poziom 4");
        var third = Add("Poziom 3", Child("next", bottom));
        var second = Add("Poziom 2", Child("next", third));
        var top = Add("Poziom 1", Child("next", second));

        Assert.That(Tree().Embeds(tiles[top], bottom), Is.True);
    }

    [Test(Description = "scenario_TEMP-041-043 - #7")]
    public void Given_ExistingCycleBelowTile_When_EmbeddingIsChecked_Then_SearchStops()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        tiles[first] = Tile("Pierwszy", Child("second", second));
        tiles[second] = Tile("Drugi", Child("first", first));

        Assert.That(Tree().Embeds(tiles[first], Guid.NewGuid()), Is.False);
    }

    [Test(Description = "scenario_TEMP-041-043 - #6")]
    public void Given_PathThroughTileChildren_When_SourceTileIsResolved_Then_DeepestTileIsReturned()
    {
        var byName = Add("Nazwa miasta");
        var filters = Add("Filtry", Child("byName", byName));
        var root = Child("filters", filters);

        Assert.That(Tree().SourceTile(root, ["filters", "byName"]), Is.SameAs(tiles[byName]));
        Assert.That(Tree().SourceTile(root, ["filters", "missing"]), Is.Null);
    }

    [Test(Description = "scenario_TEMP-041-043 - #6")]
    public void Given_PathThroughItemChild_When_SourceTileIsResolved_Then_ItIsRejected()
    {
        var line = Add("Pozycja");
        var lines = Add("Pozycje", Child("line", line, TileChildScopes.Item));
        var root = Child("lines", lines);

        Assert.That(Tree().SourceTile(root, ["lines", "line"]), Is.Null);
        Assert.That(Tree().SourceTile(Child("lines", lines, TileChildScopes.Item), ["lines"]), Is.SameAs(tiles[lines]));
    }

    [Test(Description = "scenario_TEMP-036 - #7")]
    public void Given_PathToGrandchild_When_TargetIsResolved_Then_TargetChildTileAndIntermediatesAreReturned()
    {
        var invoices = Add("Faktury");
        var orders = Add("Zamówienia", Child("invoices", invoices));

        var target = Tree().Target(Child("orders", orders), ["orders", "invoices"]);

        Assert.That(target!.Child.Key, Is.EqualTo("invoices"));
        Assert.That(target.Tile, Is.SameAs(tiles[invoices]));
        Assert.That(target.Intermediates, Is.EqualTo(new[] { tiles[orders] }));
        Assert.That(Tree().Target(Child("orders", orders), ["orders", "payments"]), Is.Null);
    }

    private TileTree Tree() => new(id => tiles.GetValueOrDefault(id));

    private Guid Add(string name, params TileChild[] children)
    {
        var id = Guid.NewGuid();
        tiles[id] = Tile(name, children);
        return id;
    }

    private static TileDefinition Tile(string name, params TileChild[] children)
        => new TileDefinition(name, TileDisplays.Details, [], null, []) { Children = children };

    private static TileChild Child(string key, Guid tileId, string scope = TileChildScopes.Tile)
        => new(key, tileId, 0, null, scope, TileChildLoads.Eager);
}
