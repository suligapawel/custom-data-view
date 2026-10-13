using System.Text.Json.Nodes;
using CustomDataView.Core.Transformations;
using CustomDataView.Core.Transformations.Suffix;

namespace CustomDataView.Core.Tests.Transformations;

public class AffixTransformationTests
{
    [Test(Description = "scenario_TEMP-001-006 - #9")]
    public void Given_SuffixWithDegreesCelsius_When_ValueIs21_5_Then_ResultIs21_5DegreesCelsius()
    {
        var transformation = new SuffixTransformation(new IdentityTransformation(), CreateText(" °C"));

        var result = transformation.Apply(21.5m);

        Assert.That(result, Is.EqualTo("21.5 °C"));
    }

    private static TransformationParameters CreateText(JsonNode text)
    {
        return new TransformationParameters(new Dictionary<string, JsonNode> { ["text"] = text });
    }
}
