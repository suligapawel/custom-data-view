using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Exceptions;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Tests.Connections;

public class ConnectionTests
{
    [Test(Description = "scenario_TEMP-007-014 - #1")]
    public void Given_ValidDefinition_When_ConnectionIsCreated_Then_ItHasNewIdAndDefinition()
    {
        var definition = Weather();

        var connection = Connection.Create(definition);

        Assert.That(connection.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(connection.Definition, Is.EqualTo(definition));
    }

    [Test(Description = "scenario_TEMP-007-014 - #7")]
    public void Given_DefinitionWithoutName_When_ConnectionIsCreated_Then_CreationIsRejectedWithViolations()
    {
        var exception = Assert.Throws<InvalidDefinitionException>(() => Connection.Create(Weather() with { Name = "" }));

        Assert.That(exception!.Violations.Keys, Is.EqualTo(new[] { "name" }));
    }

    [Test(Description = "scenario_TEMP-007-014 - #23")]
    public void Given_Connection_When_ItIsRedefined_Then_IdIsKeptAndDefinitionIsReplaced()
    {
        var connection = Connection.Create(Weather());
        var id = connection.Id;
        var replacement = new PostConnectionDefinition("Wyszukiwanie", "https://api.example.com/search", [], [], """{"q": "{q}"}""", [new ParameterDeclaration("q", true, null)]);

        connection.Redefine(replacement);

        Assert.That(connection.Id, Is.EqualTo(id));
        Assert.That(connection.Definition, Is.EqualTo(replacement));
    }

    [Test(Description = "scenario_TEMP-007-014 - #24")]
    public void Given_Connection_When_ItIsRedefinedWithInvalidDefinition_Then_PreviousDefinitionIsKept()
    {
        var definition = Weather();
        var connection = Connection.Create(definition);

        Assert.Throws<InvalidDefinitionException>(() => connection.Redefine(definition with { Url = "not-an-url" }));

        Assert.That(connection.Definition, Is.EqualTo(definition));
    }

    [Test(Description = "scenario_TEMP-015-023 - #25")]
    public void Given_ConnectionUsedByTiles_When_DeletionIsChecked_Then_ItIsRejectedWithUsingTiles()
    {
        var connection = Connection.Create(Weather());
        IReadOnlyList<TileReference> tiles = [new TileReference(Guid.NewGuid(), "Klient")];

        var exception = Assert.Throws<ConnectionInUseException>(() => connection.EnsureCanBeDeleted(tiles));

        Assert.That(exception!.ConnectionId, Is.EqualTo(connection.Id));
        Assert.That(exception.Tiles, Is.EqualTo(tiles));
    }

    [Test(Description = "scenario_TEMP-015-023 - #27")]
    public void Given_UnusedConnection_When_DeletionIsChecked_Then_ItIsAllowed()
    {
        var connection = Connection.Create(Weather());

        Assert.DoesNotThrow(() => connection.EnsureCanBeDeleted([]));
    }

    private static GetConnectionDefinition Weather()
        => new("Pogoda", "https://api.example.com/weather/{city}", [new ParameterDeclaration("city", true, null)], []);
}
