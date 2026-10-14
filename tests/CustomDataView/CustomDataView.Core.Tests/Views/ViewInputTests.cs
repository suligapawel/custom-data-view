using CustomDataView.Core.Views.Definitions;

namespace CustomDataView.Core.Tests.Views;

public class ViewInputTests
{
    [TestCase(null, null)]
    [TestCase("", null)]
    [TestCase("   ", null)]
    [TestCase("  col-6 highlight  ", "col-6 highlight")]
    [Test(Description = "scenario_TEMP-024-028 - #3")]
    public void Given_CssClass_When_InputIsConvertedToDefinition_Then_ClassIsTrimmedAndBlankBecomesNull(string cssClass, string expected)
    {
        var tileId = Guid.NewGuid();

        var definition = new ViewInput("Pulpit", [new ViewTileInput(tileId, 3, cssClass)]).ToDefinition();

        Assert.That(definition.Tiles, Is.EqualTo(new[] { new ViewTile(tileId, 3, expected) }));
    }
}
