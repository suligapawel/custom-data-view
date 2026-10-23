using CustomDataView.Domain.Services.Transformations;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Tests.Transformations;

public class NumberAsTextTests
{
    [Test(Description = "scenario_TEMP-001-006 - #13")]
    public void Given_SuffixWithKilograms_When_NumberHasTrailingZero_Then_TextHasNoTrailingZero()
    {
        var transformation = new SuffixTransformation();
        var parameter = " kg";

        var result = transformation.Apply(21.50m, parameter);

        Assert.That(result, Is.EqualTo("21.5 kg"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #13")]
    public void Given_PrefixWithHash_When_NumberIsInExponentNotation_Then_TextIsPlainNumber()
    {
        var transformation = new PrefixTransformation();
        var parameter = "#";

        var result = transformation.Apply(1e3m, parameter);

        Assert.That(result, Is.EqualTo("#1000"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #13")]
    public void Given_Uppercase_When_NumberHasTrailingZero_Then_ResultIsNumberText()
    {
        var transformation = new UppercaseTransformation();
        var parameter = (string)null;

        var result = transformation.Apply(21.50m, parameter);

        Assert.That(result, Is.EqualTo("21.5"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #13")]
    public void Given_Lowercase_When_NumberIsFraction_Then_ResultIsNumberText()
    {
        var transformation = new LowercaseTransformation();
        var parameter = (string)null;

        var result = transformation.Apply(0.1m, parameter);

        Assert.That(result, Is.EqualTo("0.1"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #13")]
    public void Given_TruncateToThree_When_NumberHasTrailingZero_Then_ResultIsCutNumberText()
    {
        var transformation = new TruncateTransformation();
        var parameter = "3";

        var result = transformation.Apply(21.50m, parameter);

        Assert.That(result, Is.EqualTo("21."));
    }
}
