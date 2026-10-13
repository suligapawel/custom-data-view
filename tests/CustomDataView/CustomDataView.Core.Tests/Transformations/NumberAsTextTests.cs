using System.Text.Json.Nodes;
using CustomDataView.Core.Transformations;
using CustomDataView.Core.Transformations.Suffix;

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

    private static TransformationParameters CreateParameter(string name, JsonNode value)
    {
        return new TransformationParameters(new Dictionary<string, JsonNode> { [name] = value });
    }
}
