using System.Text.Json.Nodes;
using CustomDataView.Core.Transformations;
using CustomDataView.Core.Transformations.Prefix;
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

    [Test(Description = "scenario_TEMP-001-006 - #9")]
    public void Given_PrefixWithIdLabel_When_ValueIs7_Then_ResultIsId7()
    {
        var transformation = new PrefixTransformation(new IdentityTransformation(), CreateText("ID: "));

        var result = transformation.Apply(7m);

        Assert.That(result, Is.EqualTo("ID: 7"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #9")]
    public void Given_PrefixWithEmptyText_When_ValueIsAbc_Then_ResultIsUnchangedText()
    {
        var transformation = new PrefixTransformation(new IdentityTransformation(), CreateText(""));

        var result = transformation.Apply("abc");

        Assert.That(result, Is.EqualTo("abc"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #10")]
    public void Given_PrefixWithoutText_When_ValueIsAbc_Then_FailsWithInvalidTextParameter()
    {
        var transformation = new PrefixTransformation(new IdentityTransformation(), new TransformationParameters(null));

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply("abc"));

        Assert.That(exception.Key, Is.EqualTo("prefix"));
        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidParameter));
        Assert.That(exception.Parameter, Is.EqualTo("text"));
    }

    private static TransformationParameters CreateText(JsonNode text)
    {
        return new TransformationParameters(new Dictionary<string, JsonNode> { ["text"] = text });
    }
}
