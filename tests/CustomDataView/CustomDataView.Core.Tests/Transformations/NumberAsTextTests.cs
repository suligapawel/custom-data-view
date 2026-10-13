using System.Text.Json.Nodes;
using CustomDataView.Core.Transformations;
using CustomDataView.Core.Transformations.Implementations;

namespace CustomDataView.Core.Tests.Transformations;

public class NumberAsTextTests
{
    [Test(Description = "scenario_TEMP-001-006 - #13")]
    public void Given_SuffixWithKilograms_When_NumberHasTrailingZero_Then_TextHasNoTrailingZero()
    {
        var transformation = new SuffixTransformation();
        var parameters = CreateParameter("text", " kg");

        var result = transformation.Apply(21.50m, parameters);

        Assert.That(result, Is.EqualTo("21.5 kg"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #13")]
    public void Given_PrefixWithHash_When_NumberIsInExponentNotation_Then_TextIsPlainNumber()
    {
        var transformation = new PrefixTransformation();
        var parameters = CreateParameter("text", "#");

        var result = transformation.Apply(1e3m, parameters);

        Assert.That(result, Is.EqualTo("#1000"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #13")]
    public void Given_Uppercase_When_NumberHasTrailingZero_Then_ResultIsNumberText()
    {
        var transformation = new UppercaseTransformation();
        var parameters = new TransformationParameters(null);

        var result = transformation.Apply(21.50m, parameters);

        Assert.That(result, Is.EqualTo("21.5"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #13")]
    public void Given_Lowercase_When_NumberIsFraction_Then_ResultIsNumberText()
    {
        var transformation = new LowercaseTransformation();
        var parameters = new TransformationParameters(null);

        var result = transformation.Apply(0.1m, parameters);

        Assert.That(result, Is.EqualTo("0.1"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #13")]
    public void Given_TruncateToThree_When_NumberHasTrailingZero_Then_ResultIsCutNumberText()
    {
        var transformation = new TruncateTransformation();
        var parameters = CreateParameter("max_length", 3);

        var result = transformation.Apply(21.50m, parameters);

        Assert.That(result, Is.EqualTo("21."));
    }

    private static TransformationParameters CreateParameter(string name, JsonNode value)
    {
        return new TransformationParameters(new Dictionary<string, JsonNode> { [name] = value });
    }
}
