using System.Text.Json.Nodes;
using CustomDataView.Core.Transformations;
using CustomDataView.Core.Transformations.DateFormat;

namespace CustomDataView.Core.Tests.Transformations;

public class DateFormatTransformationTests
{
    [Test(Description = "scenario_TEMP-001-006 - #7")]
    public void Given_DateFormatWithDateTimePattern_When_ValueIsDateTimeWithOffset_Then_ResultKeepsSourceOffset()
    {
        var transformation = CreateDateFormat("yyyy-MM-dd HH:mm");

        var result = transformation.Apply("2026-10-13T10:00:00+02:00");

        Assert.That(result, Is.EqualTo("2026-10-13 10:00"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #7")]
    public void Given_DateFormatWithDateTimePattern_When_ValueIsDateOnly_Then_ResultHasMidnight()
    {
        var transformation = CreateDateFormat("yyyy-MM-dd HH:mm");

        var result = transformation.Apply("2026-10-13");

        Assert.That(result, Is.EqualTo("2026-10-13 00:00"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #7")]
    public void Given_DateFormatWithMonthNamePattern_When_ValueIsDate_Then_MonthNameIsInInvariantCulture()
    {
        var transformation = CreateDateFormat("d MMMM yyyy");

        var result = transformation.Apply("2026-10-13");

        Assert.That(result, Is.EqualTo("13 October 2026"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #8")]
    public void Given_DateFormat_When_ValueIsNotDate_Then_FailsWithInvalidValue()
    {
        var transformation = CreateDateFormat("yyyy-MM-dd");

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply("jutro"));

        Assert.That(exception.Key, Is.EqualTo("date_format"));
        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidValue));
    }

    [Test(Description = "scenario_TEMP-001-006 - #8")]
    public void Given_DateFormat_When_ValueIsNumber_Then_FailsWithInvalidValue()
    {
        var transformation = CreateDateFormat("yyyy-MM-dd");

        var exception = Assert.Throws<TransformationFailedException>(() => transformation.Apply(1760342400m));

        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidValue));
    }

    private static DateFormatTransformation CreateDateFormat(JsonNode pattern)
    {
        var parameters = new TransformationParameters(new Dictionary<string, JsonNode> { ["pattern"] = pattern });
        return new DateFormatTransformation(new IdentityTransformation(), parameters);
    }
}
