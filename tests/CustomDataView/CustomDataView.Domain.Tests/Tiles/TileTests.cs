using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Exceptions;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Tests.Tiles;

public class TileTests
{
    private static readonly Guid ConnectionId = Guid.NewGuid();

    [Test(Description = "scenario_TEMP-015-023 - #1")]
    public void Given_ValidDefinition_When_TileIsCreated_Then_ItHasNewIdAndDefinition()
    {
        var definition = Customer("Nazwa", "Kraj");

        var tile = Tile.Create(definition);

        Assert.That(tile.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(tile.Definition, Is.EqualTo(definition));
    }

    [Test(Description = "scenario_TEMP-015-023 - #8")]
    public void Given_DefinitionWithoutFields_When_TileIsCreated_Then_CreationIsRejectedWithViolations()
    {
        var exception = Assert.Throws<InvalidDefinitionException>(() => Tile.Create(Customer()));

        Assert.That(exception!.Violations.Keys, Is.EqualTo(new[] { "fields" }));
    }

    [Test(Description = "scenario_TEMP-015-023 - #20")]
    public void Given_Tile_When_ItIsRedefined_Then_IdIsKeptAndDefinitionIsReplaced()
    {
        var tile = Tile.Create(Customer("Nazwa"));
        var id = tile.Id;
        var replacement = Customer("Nazwa", "Kraj") with { Name = "Klient VIP" };

        tile.Redefine(replacement, []);

        Assert.That(tile.Id, Is.EqualTo(id));
        Assert.That(tile.Definition, Is.EqualTo(replacement));
    }

    [Test(Description = "scenario_TEMP-015-023 - #21")]
    public void Given_Tile_When_AllFieldsAreRemoved_Then_EditIsRejectedAndDefinitionIsKept()
    {
        var definition = Customer("Nazwa");
        var tile = Tile.Create(definition);

        Assert.Throws<InvalidDefinitionException>(() => tile.Redefine(Customer(), []));

        Assert.That(tile.Definition, Is.EqualTo(definition));
    }

    [Test(Description = "scenario_TEMP-036 - #10")]
    public void Given_FieldBoundByParent_When_ItIsRenamed_Then_EditIsRejectedWithFieldAndParent()
    {
        var definition = Customer("Nazwa", "Kraj");
        var tile = Tile.Create(definition);
        var parent = new TileReference(Guid.NewGuid(), "Klient i dostawa");

        var exception = Assert.Throws<FieldInUseException>(() =>
            tile.Redefine(Customer("Nazwa", "Państwo"), [new TileFieldUse(parent, "Kraj"), new TileFieldUse(parent, "Nazwa")]));

        Assert.That(exception!.TileId, Is.EqualTo(tile.Id));
        Assert.That(exception.Fields, Is.EqualTo(new[] { "Kraj" }));
        Assert.That(exception.Tiles, Is.EqualTo(new[] { parent }));
        Assert.That(tile.Definition, Is.EqualTo(definition));
    }

    [Test(Description = "scenario_TEMP-036 - #10")]
    public void Given_FieldBoundByParent_When_ItsLabelChangesOnlyCaseAndSpaces_Then_EditIsAccepted()
    {
        var tile = Tile.Create(Customer("Nazwa", "Kraj"));
        var parent = new TileReference(Guid.NewGuid(), "Klient i dostawa");

        tile.Redefine(Customer("Nazwa", " kraj "), [new TileFieldUse(parent, "Kraj")]);

        Assert.That(tile.Definition.Fields[1].Label, Is.EqualTo(" kraj "));
    }

    [Test(Description = "scenario_TEMP-036 - #9")]
    public void Given_TileEmbeddedInParent_When_DeletionIsChecked_Then_ItIsRejectedWithParent()
    {
        var tile = Tile.Create(Customer("Nazwa"));
        var shipping = new TileReference(Guid.NewGuid(), "Dostawa");

        var exception = Assert.Throws<TileInUseException>(() => tile.EnsureCanBeDeleted([shipping], []));

        Assert.That(exception!.TileId, Is.EqualTo(tile.Id));
        Assert.That(exception.Tiles, Is.EqualTo(new[] { shipping }));
    }

    [Test(Description = "scenario_TEMP-041-043 - #21")]
    public void Given_TileEmbeddedAndTargetedByActionOfSameParent_When_DeletionIsChecked_Then_ParentIsListedOnce()
    {
        var tile = Tile.Create(Customer("Nazwa"));
        var results = new TileReference(Guid.NewGuid(), "Wyniki");
        var search = new TileReference(Guid.NewGuid(), "Wyszukiwarka");

        var exception = Assert.Throws<TileInUseException>(() => tile.EnsureCanBeDeleted([results], [results, search]));

        Assert.That(exception!.Tiles, Is.EqualTo(new[] { results, search }));
    }

    [Test(Description = "scenario_TEMP-015-023 - #23")]
    public void Given_UnreferencedTile_When_DeletionIsChecked_Then_ItIsAllowed()
    {
        var tile = Tile.Create(Customer("Nazwa"));

        Assert.DoesNotThrow(() => tile.EnsureCanBeDeleted([], []));
    }

    private static TileDefinition Customer(params string[] labels)
        => new(
            "Klient",
            TileDisplays.Details,
            [new TileSource("customer", TileSourceKinds.Connection, ConnectionId)],
            null,
            labels.Select(label => new TileField("customer", "$.value", label, [])).ToList());
}
