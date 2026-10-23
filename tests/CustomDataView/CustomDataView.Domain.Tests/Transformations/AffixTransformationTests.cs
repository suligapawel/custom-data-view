using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Exceptions;
using CustomDataView.Domain.Services.Transformations;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Tests.Transformations;

public class AffixTransformationTests
{
    [Test(Description = "scenario_TEMP-001-006 - #9")]
    public void Given_SuffixWithDegreesCelsius_When_ValueIs21_5_Then_ResultIs21_5DegreesCelsius()
    {
        var transformation = new SuffixTransformation();
        var parameter = " °C";

        var result = transformation.Apply(21.5m, parameter);

        Assert.That(result, Is.EqualTo("21.5 °C"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #9")]
    public void Given_PrefixWithIdLabel_When_ValueIs7_Then_ResultIsId7()
    {
        var transformation = new PrefixTransformation();
        var parameter = "ID: ";

        var result = transformation.Apply(7m, parameter);

        Assert.That(result, Is.EqualTo("ID: 7"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #9")]
    public void Given_PrefixWithEmptyText_When_ValueIsAbc_Then_ResultIsUnchangedText()
    {
        var transformation = new PrefixTransformation();
        var parameter = "";

        var result = transformation.Apply("abc", parameter);

        Assert.That(result, Is.EqualTo("abc"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #10")]
    public void Given_PrefixWithoutText_When_ValueIsAbc_Then_FailsWithInvalidTextParameter()
    {
        var transformation = new PrefixTransformation();
        var parameter = (string)null;

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply("abc", parameter));

        Assert.That(exception.Key, Is.EqualTo("prefix"));
        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidParameter));
        Assert.That(exception.Parameter, Is.EqualTo("text"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #10")]
    public void Given_SuffixWithoutText_When_ValueIsAbc_Then_FailsWithInvalidTextParameter()
    {
        var transformation = new SuffixTransformation();
        var parameter = (string)null;

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply("abc", parameter));

        Assert.That(exception.Key, Is.EqualTo("suffix"));
        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidParameter));
        Assert.That(exception.Parameter, Is.EqualTo("text"));
    }
}
