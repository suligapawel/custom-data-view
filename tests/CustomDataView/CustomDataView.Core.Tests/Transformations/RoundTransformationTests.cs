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

    [Test(Description = "scenario_TEMP-001-006 - #4")]
    public void Given_RoundWithTwoDecimals_When_ValueIsTextWithSurroundingSpaces_Then_ResultIs21_46()
    {
        var transformation = CreateRound(2);

        var result = transformation.Apply(" 21.4567 ");

        Assert.That(result, Is.EqualTo(21.46m));
    }

    [Test(Description = "scenario_TEMP-001-006 - #5")]
    public void Given_RoundWithTwoDecimals_When_ValueIsAbc_Then_FailsWithInvalidValue()
    {
        var transformation = CreateRound(2);

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply("abc"));

        Assert.That(exception.Key, Is.EqualTo("round"));
        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidValue));
    }

    [Test(Description = "scenario_TEMP-001-006 - #5")]
    public void Given_RoundWithTwoDecimals_When_ValueHasThousandsAndDecimalSeparator_Then_FailsWithInvalidValue()
    {
        var transformation = CreateRound(2);

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply("1,234.5"));

        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidValue));
    }

    [Test(Description = "scenario_TEMP-001-006 - #5")]
    public void Given_RoundWithTwoDecimals_When_ValueIsBoolean_Then_FailsWithInvalidValue()
    {
        var transformation = CreateRound(2);

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply(true));

        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidValue));
    }

    [Test(Description = "scenario_TEMP-001-006 - #6")]
    public void Given_RoundWithoutDecimals_When_ValueIsTransformed_Then_FailsWithInvalidDecimalsParameter()
    {
        var transformation = new RoundTransformation(new IdentityTransformation(), new TransformationParameters(null));

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply(21.4567m));

        Assert.That(exception.Key, Is.EqualTo("round"));
        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidParameter));
        Assert.That(exception.Parameter, Is.EqualTo("decimals"));
    }

    private static RoundTransformation CreateRound(JsonNode decimals)
    {
        var parameters = new TransformationParameters(new Dictionary<string, JsonNode> { ["decimals"] = decimals });
        return new RoundTransformation(new IdentityTransformation(), parameters);
    }
}
