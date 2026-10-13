using System.Text.Json.Nodes;
using CustomDataView.Core.Transformations;
using CustomDataView.Core.Transformations.Lowercase;
using CustomDataView.Core.Transformations.Prefix;
using CustomDataView.Core.Transformations.Suffix;
using CustomDataView.Core.Transformations.Truncate;
using CustomDataView.Core.Transformations.Uppercase;

namespace CustomDataView.Core.Tests.Transformations;

public class NumberAsTextTests
{
    [Test(Description = "scenario_TEMP-001-006 - #13")]
    public void Given_SuffixWithKilograms_When_NumberHasTrailingZero_Then_TextHasNoTrailingZero()
    {
        var transformation = new SuffixTransformation(new IdentityTransformation(), CreateParameter("text", " kg"));

        var result = transformation.Apply(21.50m);

        Assert.That(result, Is.EqualTo("21.5 kg"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #13")]
    public void Given_PrefixWithHash_When_NumberIsInExponentNotation_Then_TextIsPlainNumber()
    {
        var transformation = new PrefixTransformation(new IdentityTransformation(), CreateParameter("text", "#"));

        var result = transformation.Apply(1e3m);

        Assert.That(result, Is.EqualTo("#1000"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #13")]
    public void Given_Uppercase_When_NumberHasTrailingZero_Then_ResultIsNumberText()
    {
        var transformation = new UppercaseTransformation(new IdentityTransformation(), new TransformationParameters(null));

        var result = transformation.Apply(21.50m);

        Assert.That(result, Is.EqualTo("21.5"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #13")]
    public void Given_Lowercase_When_NumberIsFraction_Then_ResultIsNumberText()
    {
        var transformation = new LowercaseTransformation(new IdentityTransformation(), new TransformationParameters(null));

        var result = transformation.Apply(0.1m);

        Assert.That(result, Is.EqualTo("0.1"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #13")]
    public void Given_TruncateToThree_When_NumberHasTrailingZero_Then_ResultIsCutNumberText()
    {
        var transformation = new TruncateTransformation(new IdentityTransformation(), CreateParameter("max_length", 3));

        var result = transformation.Apply(21.50m);

        Assert.That(result, Is.EqualTo("21."));
    }

    private static TransformationParameters CreateParameter(string name, JsonNode value)
    {
        return new TransformationParameters(new Dictionary<string, JsonNode> { [name] = value });
    }
}
