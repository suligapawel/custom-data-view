using System.Text.Json.Nodes;
using CustomDataView.Core.Transformations.Abstractions;
using CustomDataView.Core.Transformations.Implementations;

namespace CustomDataView.Core.Tests.Transformations;

public class DateFormatTransformationTests
{
    [Test(Description = "scenario_TEMP-001-006 - #7")]
    public void Given_DateFormatWithDateTimePattern_When_ValueIsDateTimeWithOffset_Then_ResultKeepsSourceOffset()
    {
        var transformation = new DateFormatTransformation();
        var parameters = Pattern("yyyy-MM-dd HH:mm");

        var result = transformation.Apply("2026-10-13T10:00:00+02:00", parameters);

        Assert.That(result, Is.EqualTo("2026-10-13 10:00"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #7")]
    public void Given_DateFormatWithDateTimePattern_When_ValueIsDateOnly_Then_ResultHasMidnight()
    {
        var transformation = new DateFormatTransformation();
        var parameters = Pattern("yyyy-MM-dd HH:mm");

        var result = transformation.Apply("2026-10-13", parameters);

        Assert.That(result, Is.EqualTo("2026-10-13 00:00"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #7")]
    public void Given_DateFormatWithMonthNamePattern_When_ValueIsDate_Then_MonthNameIsInInvariantCulture()
    {
        var transformation = new DateFormatTransformation();
        var parameters = Pattern("d MMMM yyyy");

        var result = transformation.Apply("2026-10-13", parameters);

        Assert.That(result, Is.EqualTo("13 October 2026"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #8")]
    public void Given_DateFormat_When_ValueIsNotDate_Then_FailsWithInvalidValue()
    {
        var transformation = new DateFormatTransformation();
        var parameters = Pattern("yyyy-MM-dd");

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply("jutro", parameters));

        Assert.That(exception.Key, Is.EqualTo("date_format"));
        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidValue));
    }

    [Test(Description = "scenario_TEMP-001-006 - #8")]
    public void Given_DateFormat_When_ValueIsNumber_Then_FailsWithInvalidValue()
    {
        var transformation = new DateFormatTransformation();
        var parameters = Pattern("yyyy-MM-dd");

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply(1760342400m, parameters));

        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidValue));
    }

    [Test(Description = "scenario_TEMP-001-006 - #8")]
    public void Given_DateFormatWithEmptyPattern_When_ValueIsDate_Then_FailsWithInvalidPatternParameter()
    {
        var transformation = new DateFormatTransformation();
        var parameters = Pattern("");

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply("2026-10-13", parameters));

        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidParameter));
        Assert.That(exception.Parameter, Is.EqualTo("pattern"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #8")]
    public void Given_DateFormatWithoutPattern_When_ValueIsDate_Then_FailsWithInvalidPatternParameter()
    {
        var transformation = new DateFormatTransformation();
        var parameters = new TransformationParameters(null);

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply("2026-10-13", parameters));

        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidParameter));
        Assert.That(exception.Parameter, Is.EqualTo("pattern"));
    }

    private static TransformationParameters Pattern(JsonNode pattern)
    {
        return new TransformationParameters(new Dictionary<string, JsonNode> { ["pattern"] = pattern });
    }
}
