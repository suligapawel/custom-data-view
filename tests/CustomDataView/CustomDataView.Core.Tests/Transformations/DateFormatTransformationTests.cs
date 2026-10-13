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

    private static DateFormatTransformation CreateDateFormat(JsonNode pattern)
    {
        var parameters = new TransformationParameters(new Dictionary<string, JsonNode> { ["pattern"] = pattern });
        return new DateFormatTransformation(new IdentityTransformation(), parameters);
    }
}
