using System.Text.Json.Nodes;
using CustomDataView.Core.Transformations;
using CustomDataView.Core.Transformations.Implementations;

namespace CustomDataView.Core.Tests.Transformations;

public class TextTransformationTests
{
    [Test(Description = "scenario_TEMP-001-006 - #11")]
    public void Given_Uppercase_When_ValueIsAbc_Then_ResultIsUppercaseText()
    {
        var transformation = new UppercaseTransformation();
        var parameters = new TransformationParameters(null);

        var result = transformation.Apply("abc", parameters);

        Assert.That(result, Is.EqualTo("ABC"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #11")]
    [SetCulture("tr-TR")]
    public void Given_UppercaseOnTurkishServerCulture_When_ValueIsIstanbul_Then_ResultUsesInvariantCulture()
    {
        var transformation = new UppercaseTransformation();
        var parameters = new TransformationParameters(null);

        var result = transformation.Apply("istanbul", parameters);

        Assert.That(result, Is.EqualTo("ISTANBUL"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #11")]
    public void Given_Lowercase_When_ValueIsAbc_Then_ResultIsLowercaseText()
    {
        var transformation = new LowercaseTransformation();
        var parameters = new TransformationParameters(null);

        var result = transformation.Apply("ABC", parameters);

        Assert.That(result, Is.EqualTo("abc"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #11")]
    [SetCulture("tr-TR")]
    public void Given_LowercaseOnTurkishServerCulture_When_ValueIsTitle_Then_ResultUsesInvariantCulture()
    {
        var transformation = new LowercaseTransformation();
        var parameters = new TransformationParameters(null);

        var result = transformation.Apply("TITLE", parameters);

        Assert.That(result, Is.EqualTo("title"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #11")]
    public void Given_TruncateToThree_When_ValueIsLonger_Then_ResultIsCutToThreeCharacters()
    {
        var transformation = new TruncateTransformation();
        var parameters = MaxLength(3);

        var result = transformation.Apply("abcdef", parameters);

        Assert.That(result, Is.EqualTo("abc"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #11")]
    public void Given_TruncateToThree_When_ValueIsShorter_Then_ResultIsUnchanged()
    {
        var transformation = new TruncateTransformation();
        var parameters = MaxLength(3);

        var result = transformation.Apply("ab", parameters);

        Assert.That(result, Is.EqualTo("ab"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #12")]
    public void Given_TruncateWithoutMaxLength_When_ValueIsTransformed_Then_FailsWithInvalidMaxLengthParameter()
    {
        var transformation = new TruncateTransformation();
        var parameters = new TransformationParameters(null);

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply("abcdef", parameters));

        Assert.That(exception.Key, Is.EqualTo("truncate"));
        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidParameter));
        Assert.That(exception.Parameter, Is.EqualTo("max_length"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #12")]
    public void Given_TruncateWithZeroMaxLength_When_ValueIsTransformed_Then_FailsWithInvalidMaxLengthParameter()
    {
        var transformation = new TruncateTransformation();
        var parameters = MaxLength(0);

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply("abcdef", parameters));

        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidParameter));
        Assert.That(exception.Parameter, Is.EqualTo("max_length"));
    }

    private static TransformationParameters MaxLength(JsonNode maxLength)
    {
        return new TransformationParameters(new Dictionary<string, JsonNode> { ["max_length"] = maxLength });
    }
}
