using System.Text.Json.Nodes;
using CustomDataView.Core.Transformations;
using CustomDataView.Core.Transformations.Round;

namespace CustomDataView.Core.Tests.Transformations;

public class RoundTransformationTests
{
    [Test(Description = "scenario_TEMP-001-006 - #4")]
    public void Given_RoundWithTwoDecimals_When_ValueIs21_4567_Then_ResultIs21_46()
    {
        var transformation = CreateRound(2);

        var result = transformation.Apply(21.4567m);

        Assert.That(result, Is.EqualTo(21.46m));
    }

    [Test(Description = "scenario_TEMP-001-006 - #4")]
    public void Given_RoundWithZeroDecimals_When_ValueIs21_4567_Then_ResultIs21()
    {
        var transformation = CreateRound(0);

        var result = transformation.Apply(21.4567m);

        Assert.That(result, Is.EqualTo(21m));
    }

    [Test(Description = "scenario_TEMP-001-006 - #4")]
    public void Given_RoundWithZeroDecimals_When_ValueIs2_5_Then_ResultIs3()
    {
        var transformation = CreateRound(0);

        var result = transformation.Apply(2.5m);

        Assert.That(result, Is.EqualTo(3m));
    }

    [Test(Description = "scenario_TEMP-001-006 - #4")]
    public void Given_RoundWithZeroDecimals_When_ValueIsMinus2_5_Then_ResultIsMinus3()
    {
        var transformation = CreateRound(0);

        var result = transformation.Apply(-2.5m);

        Assert.That(result, Is.EqualTo(-3m));
    }

    [Test(Description = "scenario_TEMP-001-006 - #4")]
    public void Given_RoundWithTwoDecimals_When_ValueIsTextWithComma_Then_ResultIs21_46()
    {
        var transformation = CreateRound(2);

        var result = transformation.Apply("21,4567");

        Assert.That(result, Is.EqualTo(21.46m));
    }

    private static RoundTransformation CreateRound(JsonNode decimals)
    {
        var parameters = new TransformationParameters(new Dictionary<string, JsonNode> { ["decimals"] = decimals });
        return new RoundTransformation(new IdentityTransformation(), parameters);
    }
}
