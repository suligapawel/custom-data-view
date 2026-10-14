using CustomDataView.Core.Views.Definitions;
using CustomDataView.Core.Views.Validation;

namespace CustomDataView.Core.Tests.Views;

public class ViewDefinitionValidatorTests
{
    private static readonly Guid CustomerTileId = Guid.NewGuid();
    private static readonly Guid RateTileId = Guid.NewGuid();

    [Test(Description = "scenario_TEMP-024-028 - #2")]
    public void Given_NamedViewWithoutTiles_When_Validated_Then_ThereAreNoErrors()
    {
        var errors = ViewDefinitionValidator.Validate(new ViewInput("Pusty", []));

        Assert.That(errors, Is.Empty);
    }

    [TestCase(null)]
    [TestCase("   ")]
    [TestCase(101)]
    [Test(Description = "scenario_TEMP-024-028 - #9")]
    public void Given_InvalidName_When_Validated_Then_NameHasError(object name)
    {
        var errors = ViewDefinitionValidator.Validate(new ViewInput(name is int length ? new string('a', length) : (string)name, []));

        Assert.That(errors.Keys, Is.EqualTo(new[] { "name" }));
    }

    [Test(Description = "scenario_TEMP-024-028 - #9")]
    public void Given_NameOf100Characters_When_Validated_Then_ThereAreNoErrors()
    {
        var errors = ViewDefinitionValidator.Validate(new ViewInput(new string('a', 100), []));

        Assert.That(errors, Is.Empty);
    }

    [Test(Description = "scenario_TEMP-024-028 - #10")]
    public void Given_EntryWithoutTile_When_Validated_Then_EntryTileHasError()
    {
        var errors = ViewDefinitionValidator.Validate(new ViewInput("Pulpit", [new ViewTileInput(null, 0, null)]));

        Assert.That(errors.Keys, Is.EqualTo(new[] { "tiles[0].tileId" }));
    }

    [Test(Description = "scenario_TEMP-024-028 - #12")]
    public void Given_TileThreeTimes_When_Validated_Then_OnlyRepetitionsHaveErrors()
    {
        var errors = ViewDefinitionValidator.Validate(new ViewInput("Pulpit",
        [
            new ViewTileInput(CustomerTileId, 0, null),
            new ViewTileInput(CustomerTileId, 1, null),
            new ViewTileInput(CustomerTileId, 2, null)
        ]));

        Assert.That(errors.Keys, Is.EquivalentTo(new[] { "tiles[1].tileId", "tiles[2].tileId" }));
    }

    [TestCase(null)]
    [TestCase(-1)]
    [Test(Description = "scenario_TEMP-024-028 - #13")]
    public void Given_MissingOrNegativePosition_When_Validated_Then_PositionHasError(int? position)
    {
        var errors = ViewDefinitionValidator.Validate(new ViewInput("Pulpit", [new ViewTileInput(CustomerTileId, position, null)]));

        Assert.That(errors.Keys, Is.EqualTo(new[] { "tiles[0].position" }));
    }

    [Test(Description = "scenario_TEMP-024-028 - #4")]
    public void Given_PositionsWithGaps_When_Validated_Then_ThereAreNoErrors()
    {
        var errors = ViewDefinitionValidator.Validate(new ViewInput("Pulpit",
            [new ViewTileInput(CustomerTileId, 10, null), new ViewTileInput(RateTileId, 0, null)]));

        Assert.That(errors, Is.Empty);
    }

    [Test(Description = "scenario_TEMP-024-028 - #14")]
    public void Given_RepeatedPosition_When_Validated_Then_OnlyRepetitionHasError()
    {
        var errors = ViewDefinitionValidator.Validate(new ViewInput("Pulpit",
            [new ViewTileInput(CustomerTileId, 2, null), new ViewTileInput(RateTileId, 2, null)]));

        Assert.That(errors.Keys, Is.EqualTo(new[] { "tiles[1].position" }));
    }

    [TestCase(200, false)]
    [TestCase(201, true)]
    [Test(Description = "scenario_TEMP-024-028 - #15")]
    public void Given_PaddedCssClassOfGivenLength_When_Validated_Then_OnlyLongerThan200HasError(int length, bool rejected)
    {
        var errors = ViewDefinitionValidator.Validate(new ViewInput("Pulpit",
            [new ViewTileInput(CustomerTileId, 0, $"  {new string('c', length)}  ")]));

        Assert.That(errors.ContainsKey("tiles[0].cssClass"), Is.EqualTo(rejected));
    }
}
