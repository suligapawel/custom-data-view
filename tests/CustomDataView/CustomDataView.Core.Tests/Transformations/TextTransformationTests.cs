using System.Text.Json.Nodes;
using CustomDataView.Core.Transformations;
using CustomDataView.Core.Transformations.Lowercase;
using CustomDataView.Core.Transformations.Truncate;
using CustomDataView.Core.Transformations.Uppercase;

namespace CustomDataView.Core.Tests.Transformations;

public class TextTransformationTests
{
    [Test(Description = "scenario_TEMP-001-006 - #11")]
    public void Given_Uppercase_When_ValueIsAbc_Then_ResultIsUppercaseText()
    {
        var transformation = new UppercaseTransformation(new IdentityTransformation(), new TransformationParameters(null));

        var result = transformation.Apply("abc");

        Assert.That(result, Is.EqualTo("ABC"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #11")]
    [SetCulture("tr-TR")]
    public void Given_UppercaseOnTurkishServerCulture_When_ValueIsIstanbul_Then_ResultUsesInvariantCulture()
    {
        var transformation = new UppercaseTransformation(new IdentityTransformation(), new TransformationParameters(null));

        var result = transformation.Apply("istanbul");

        Assert.That(result, Is.EqualTo("ISTANBUL"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #11")]
    public void Given_Lowercase_When_ValueIsAbc_Then_ResultIsLowercaseText()
    {
        var transformation = new LowercaseTransformation(new IdentityTransformation(), new TransformationParameters(null));

        var result = transformation.Apply("ABC");

        Assert.That(result, Is.EqualTo("abc"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #11")]
    [SetCulture("tr-TR")]
    public void Given_LowercaseOnTurkishServerCulture_When_ValueIsTitle_Then_ResultUsesInvariantCulture()
    {
        var transformation = new LowercaseTransformation(new IdentityTransformation(), new TransformationParameters(null));

        var result = transformation.Apply("TITLE");

        Assert.That(result, Is.EqualTo("title"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #11")]
    public void Given_TruncateToThree_When_ValueIsLonger_Then_ResultIsCutToThreeCharacters()
    {
        var transformation = CreateTruncate(3);

        var result = transformation.Apply("abcdef");

        Assert.That(result, Is.EqualTo("abc"));
    }

    private static TruncateTransformation CreateTruncate(JsonNode maxLength)
    {
        var parameters = new TransformationParameters(new Dictionary<string, JsonNode> { ["max_length"] = maxLength });
        return new TruncateTransformation(new IdentityTransformation(), parameters);
    }
}
