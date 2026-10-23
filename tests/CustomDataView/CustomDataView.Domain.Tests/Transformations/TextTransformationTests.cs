using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Exceptions;
using CustomDataView.Domain.Services.Transformations;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Tests.Transformations;

public class TextTransformationTests
{
    [Test(Description = "scenario_TEMP-001-006 - #11")]
    public void Given_Uppercase_When_ValueIsAbc_Then_ResultIsUppercaseText()
    {
        var transformation = new UppercaseTransformation();
        var parameter = (string)null;

        var result = transformation.Apply("abc", parameter);

        Assert.That(result, Is.EqualTo("ABC"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #11")]
    [SetCulture("tr-TR")]
    public void Given_UppercaseOnTurkishServerCulture_When_ValueIsIstanbul_Then_ResultUsesInvariantCulture()
    {
        var transformation = new UppercaseTransformation();
        var parameter = (string)null;

        var result = transformation.Apply("istanbul", parameter);

        Assert.That(result, Is.EqualTo("ISTANBUL"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #11")]
    public void Given_Lowercase_When_ValueIsAbc_Then_ResultIsLowercaseText()
    {
        var transformation = new LowercaseTransformation();
        var parameter = (string)null;

        var result = transformation.Apply("ABC", parameter);

        Assert.That(result, Is.EqualTo("abc"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #11")]
    [SetCulture("tr-TR")]
    public void Given_LowercaseOnTurkishServerCulture_When_ValueIsTitle_Then_ResultUsesInvariantCulture()
    {
        var transformation = new LowercaseTransformation();
        var parameter = (string)null;

        var result = transformation.Apply("TITLE", parameter);

        Assert.That(result, Is.EqualTo("title"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #11")]
    public void Given_TruncateToThree_When_ValueIsLonger_Then_ResultIsCutToThreeCharacters()
    {
        var transformation = new TruncateTransformation();
        var parameter = "3";

        var result = transformation.Apply("abcdef", parameter);

        Assert.That(result, Is.EqualTo("abc"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #11")]
    public void Given_TruncateToThree_When_ValueIsShorter_Then_ResultIsUnchanged()
    {
        var transformation = new TruncateTransformation();
        var parameter = "3";

        var result = transformation.Apply("ab", parameter);

        Assert.That(result, Is.EqualTo("ab"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #12")]
    public void Given_TruncateWithoutMaxLength_When_ValueIsTransformed_Then_FailsWithInvalidMaxLengthParameter()
    {
        var transformation = new TruncateTransformation();
        var parameter = (string)null;

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply("abcdef", parameter));

        Assert.That(exception.Key, Is.EqualTo("truncate"));
        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidParameter));
        Assert.That(exception.Parameter, Is.EqualTo("max_length"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #12")]
    public void Given_TruncateWithZeroMaxLength_When_ValueIsTransformed_Then_FailsWithInvalidMaxLengthParameter()
    {
        var transformation = new TruncateTransformation();
        var parameter = "0";

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply("abcdef", parameter));

        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidParameter));
        Assert.That(exception.Parameter, Is.EqualTo("max_length"));
    }
}
