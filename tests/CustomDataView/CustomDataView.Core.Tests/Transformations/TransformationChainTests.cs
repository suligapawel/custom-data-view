using System.Text.Json.Nodes;
using CustomDataView.Core.Transformations;
using CustomDataView.Core.Transformations.Catalog;
using CustomDataView.Core.Transformations.Chain;
using CustomDataView.Core.Transformations.Prefix;
using CustomDataView.Core.Transformations.Round;
using CustomDataView.Core.Transformations.Suffix;
using CustomDataView.Core.Transformations.Uppercase;

namespace CustomDataView.Core.Tests.Transformations;

public class TransformationChainTests
{
    [Test(Description = "scenario_TEMP-001-006 - #15")]
    public void Given_RoundThenSuffix_When_ValueIs21_46_Then_TransformationsAreAppliedInListOrder()
    {
        var executor = CreateExecutor();
        var steps = new[]
        {
            Step("round", ("decimals", 1)),
            Step("suffix", ("text", " °C"))
        };

        var result = executor.Execute(21.46m, steps);

        Assert.That(result.Value, Is.EqualTo("21.5 °C"));
    }

    [Test(Description = "scenario_TEMP-001-006 - #16")]
    public void Given_NoTransformations_When_ValueIsTransformed_Then_ValueIsUnchanged()
    {
        var executor = CreateExecutor();

        var result = executor.Execute(21.4567m, []);

        Assert.That(result.Value, Is.EqualTo(21.4567m));
    }

    [Test(Description = "scenario_TEMP-001-006 - #17")]
    public void Given_ChainWithRoundInTheMiddle_When_RoundFails_Then_ChainStopsWithRoundFailure()
    {
        var executor = CreateExecutor();
        var steps = new[]
        {
            Step("uppercase"),
            Step("round", ("decimals", 1)),
            Step("suffix", ("text", " °C"))
        };

        var result = executor.Execute("abc", steps);

        Assert.That(result.Error, Is.EqualTo(new TransformationChainError("transformation_failed", "round", "invalid_value")));
        Assert.That(result.Value, Is.Null);
    }

    [Test(Description = "scenario_TEMP-001-006 - #18")]
    public void Given_ChainWithTransformationOutsideCatalog_When_ValueIsTransformed_Then_FailsWithUnknownTransformation()
    {
        var executor = CreateExecutor();
        var steps = new[]
        {
            Step("uppercase"),
            Step("reverse")
        };

        var result = executor.Execute("abc", steps);

        Assert.That(result.Error, Is.EqualTo(new TransformationChainError("transformation_failed", "reverse", "unknown_transformation")));
    }

    [Test(Description = "scenario_TEMP-001-006 - #19")]
    public void Given_SamePrefixTwice_When_ValueIsX_Then_BothPrefixesAreApplied()
    {
        var executor = CreateExecutor();
        var steps = new[]
        {
            Step("prefix", ("text", "a")),
            Step("prefix", ("text", "b"))
        };

        var result = executor.Execute("x", steps);

        Assert.That(result.Value, Is.EqualTo("bax"));
    }

    private static TransformationChainExecutor CreateExecutor()
    {
        ITransformationDefinition[] definitions =
        [
            new PrefixTransformationDefinition(),
            new RoundTransformationDefinition(),
            new SuffixTransformationDefinition(),
            new UppercaseTransformationDefinition()
        ];
        return new TransformationChainExecutor(new TransformationChainBuilder(new TransformationCatalog(definitions)));
    }

    private static TransformationStep Step(string key, params (string Name, JsonNode Value)[] parameters)
    {
        return new TransformationStep(key, parameters.ToDictionary(parameter => parameter.Name, parameter => parameter.Value));
    }
}
