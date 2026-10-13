using CustomDataView.Core.Transformations;
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
}
