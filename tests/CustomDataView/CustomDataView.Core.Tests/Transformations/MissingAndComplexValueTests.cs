using System.Text.Json.Nodes;
using CustomDataView.Core.Transformations;
using CustomDataView.Core.Transformations.Round;
using CustomDataView.Core.Transformations.Truncate;
using CustomDataView.Core.Transformations.Uppercase;

namespace CustomDataView.Core.Tests.Transformations;

public class MissingAndComplexValueTests
{
    [Test(Description = "scenario_TEMP-001-006 - #14")]
    public void Given_Uppercase_When_ValueIsMissing_Then_ResultIsMissing()
    {
        var transformation = new UppercaseTransformation(new IdentityTransformation(), new TransformationParameters(null));

        var result = transformation.Apply(null);

        Assert.That(result, Is.Null);
    }

    [Test(Description = "scenario_TEMP-001-006 - #14")]
    public void Given_RoundWithTwoDecimals_When_ValueIsMissing_Then_ResultIsMissing()
    {
        var parameters = new TransformationParameters(new Dictionary<string, JsonNode> { ["decimals"] = 2 });
        var transformation = new RoundTransformation(new IdentityTransformation(), parameters);

        var result = transformation.Apply(null);

        Assert.That(result, Is.Null);
    }
}
