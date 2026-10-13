using System.Text.Json.Nodes;
using CustomDataView.Core.Transformations.Abstractions;
using CustomDataView.Core.Transformations.Execution;
using CustomDataView.Core.Transformations.Implementations;

namespace CustomDataView.Core.Tests.Transformations;

public class TransformationChainTests
{
    [Test(Description = "scenario_TEMP-001-006 - #15")]
    public void Given_RoundThenSuffix_When_ValueIs21_46_Then_TransformationsAreAppliedInListOrder()
    {
        var service = CreateService();
        var steps = new[]
        {
            Step("round", ("decimals", 1)),
            Step("suffix", ("text", " °C"))
        };

        var result = service.Apply(21.46m, steps);

        Assert.That(result.Value, Is.EqualTo("21.5 °C"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #16")]
    public void Given_NoTransformations_When_ValueIsTransformed_Then_ValueIsUnchanged()
    {
        var service = CreateService();

        var result = service.Apply(21.4567m, []);

        Assert.That(result.Value, Is.EqualTo(21.4567m));
    }

    [Test(Description = "scenario_TEMP-001-006 - #17")]
    public void Given_ChainWithRoundInTheMiddle_When_RoundFails_Then_ChainStopsWithRoundFailure()
    {
        var service = CreateService();
        var steps = new[]
        {
            Step("uppercase"),
            Step("round", ("decimals", 1)),
            Step("suffix", ("text", " °C"))
        };

        var result = service.Apply("abc", steps);

        Assert.That(result.Error, Is.EqualTo(new TransformationError("transformation_failed", "round", "invalid_value")));
        Assert.That(result.Value, Is.Null);
    }

    [Test(Description = "scenario_TEMP-001-006 - #18")]
    public void Given_ChainWithTransformationOutsideCatalog_When_ValueIsTransformed_Then_FailsWithUnknownTransformation()
    {
        var service = CreateService();
        var steps = new[]
        {
            Step("uppercase"),
            Step("reverse")
        };

        var result = service.Apply("abc", steps);

        Assert.That(result.Error, Is.EqualTo(new TransformationError("transformation_failed", "reverse", "unknown_transformation")));
    }

    [Test(Description = "scenario_TEMP-001-006 - #19")]
    public void Given_SamePrefixTwice_When_ValueIsX_Then_BothPrefixesAreApplied()
    {
        var service = CreateService();
        var steps = new[]
        {
            Step("prefix", ("text", "a")),
            Step("prefix", ("text", "b"))
        };

        var result = service.Apply("x", steps);

        Assert.That(result.Value, Is.EqualTo("bax"));
    }

    private static TransformationService CreateService()
    {
        ITransformation[] transformations =
        [
            new PrefixTransformation(),
            new RoundTransformation(),
            new SuffixTransformation(),
            new UppercaseTransformation()
        ];
        return new TransformationService(transformations);
    }

    private static TransformationStep Step(string key, params (string Name, JsonNode Value)[] parameters)
    {
        return new TransformationStep(key, parameters.ToDictionary(parameter => parameter.Name, parameter => parameter.Value));
    }
}
