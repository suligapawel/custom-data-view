using System.Text.Json.Nodes;
using CustomDataView.Core.Transformations.Abstractions;
using CustomDataView.Core.Transformations.Implementations;

namespace CustomDataView.Core.Tests.Transformations;

public class RoundTransformationTests
{
    [Test(Description = "scenario_TEMP-001-006 - #4")]
    public void Given_RoundWithTwoDecimals_When_ValueIs21_4567_Then_ResultIs21_46()
    {
        var transformation = new RoundTransformation();
        var parameters = Decimals(2);

        var result = transformation.Apply(21.4567m, parameters);

        Assert.That(result, Is.EqualTo(21.46m));
    }

    [Test(Description = "scenario_TEMP-001-006 - #4")]
    public void Given_RoundWithZeroDecimals_When_ValueIs21_4567_Then_ResultIs21()
    {
        var transformation = new RoundTransformation();
        var parameters = Decimals(0);

        var result = transformation.Apply(21.4567m, parameters);

        Assert.That(result, Is.EqualTo(21m));
    }

    [Test(Description = "scenario_TEMP-001-006 - #4")]
    public void Given_RoundWithZeroDecimals_When_ValueIs2_5_Then_ResultIs3()
    {
        var transformation = new RoundTransformation();
        var parameters = Decimals(0);

        var result = transformation.Apply(2.5m, parameters);

        Assert.That(result, Is.EqualTo(3m));
    }

    [Test(Description = "scenario_TEMP-001-006 - #4")]
    public void Given_RoundWithZeroDecimals_When_ValueIsMinus2_5_Then_ResultIsMinus3()
    {
        var transformation = new RoundTransformation();
        var parameters = Decimals(0);

        var result = transformation.Apply(-2.5m, parameters);

        Assert.That(result, Is.EqualTo(-3m));
    }

    [Test(Description = "scenario_TEMP-001-006 - #4")]
    public void Given_RoundWithTwoDecimals_When_ValueIsTextWithComma_Then_ResultIs21_46()
    {
        var transformation = new RoundTransformation();
        var parameters = Decimals(2);

        var result = transformation.Apply("21,4567", parameters);

        Assert.That(result, Is.EqualTo(21.46m));
    }

    [Test(Description = "scenario_TEMP-001-006 - #4")]
    public void Given_RoundWithTwoDecimals_When_ValueIsTextWithSurroundingSpaces_Then_ResultIs21_46()
    {
        var transformation = new RoundTransformation();
        var parameters = Decimals(2);

        var result = transformation.Apply(" 21.4567 ", parameters);

        Assert.That(result, Is.EqualTo(21.46m));
    }

    [Test(Description = "scenario_TEMP-001-006 - #5")]
    public void Given_RoundWithTwoDecimals_When_ValueIsAbc_Then_FailsWithInvalidValue()
    {
        var transformation = new RoundTransformation();
        var parameters = Decimals(2);

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply("abc", parameters));

        Assert.That(exception.Key, Is.EqualTo("round"));
        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidValue));
    }

    [Test(Description = "scenario_TEMP-001-006 - #5")]
    public void Given_RoundWithTwoDecimals_When_ValueHasThousandsAndDecimalSeparator_Then_FailsWithInvalidValue()
    {
        var transformation = new RoundTransformation();
        var parameters = Decimals(2);

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply("1,234.5", parameters));

        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidValue));
    }

    [Test(Description = "scenario_TEMP-001-006 - #5")]
    public void Given_RoundWithTwoDecimals_When_ValueIsBoolean_Then_FailsWithInvalidValue()
    {
        var transformation = new RoundTransformation();
        var parameters = Decimals(2);

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply(true, parameters));

        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidValue));
    }

    [Test(Description = "scenario_TEMP-001-006 - #6")]
    public void Given_RoundWithoutDecimals_When_ValueIsTransformed_Then_FailsWithInvalidDecimalsParameter()
    {
        var transformation = new RoundTransformation();
        var parameters = new TransformationParameters(null);

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply(21.4567m, parameters));

        Assert.That(exception.Key, Is.EqualTo("round"));
        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidParameter));
        Assert.That(exception.Parameter, Is.EqualTo("decimals"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #6")]
    public void Given_RoundWithNegativeDecimals_When_ValueIsTransformed_Then_FailsWithInvalidDecimalsParameter()
    {
        var transformation = new RoundTransformation();
        var parameters = Decimals(-1);

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply(21.4567m, parameters));

        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidParameter));
        Assert.That(exception.Parameter, Is.EqualTo("decimals"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #6")]
    public void Given_RoundWithElevenDecimals_When_ValueIsTransformed_Then_FailsWithInvalidDecimalsParameter()
    {
        var transformation = new RoundTransformation();
        var parameters = Decimals(11);

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply(21.4567m, parameters));

        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidParameter));
        Assert.That(exception.Parameter, Is.EqualTo("decimals"));
    }

    private static TransformationParameters Decimals(JsonNode decimals)
    {
        return new TransformationParameters(new Dictionary<string, JsonNode> { ["decimals"] = decimals });
    }
}
