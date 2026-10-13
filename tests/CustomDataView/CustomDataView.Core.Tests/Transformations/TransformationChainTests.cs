using System.Text.Json.Nodes;
using CustomDataView.Core.Transformations;
using CustomDataView.Core.Transformations.Catalog;
using CustomDataView.Core.Transformations.Chain;
using CustomDataView.Core.Transformations.Round;
using CustomDataView.Core.Transformations.Suffix;

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

    private static TransformationChainExecutor CreateExecutor()
    {
        ITransformationDefinition[] definitions =
        [
            new RoundTransformationDefinition(),
            new SuffixTransformationDefinition()
        ];
        return new TransformationChainExecutor(new TransformationChainBuilder(new TransformationCatalog(definitions)));
    }

    private static TransformationStep Step(string key, params (string Name, JsonNode Value)[] parameters)
    {
        return new TransformationStep(key, parameters.ToDictionary(parameter => parameter.Name, parameter => parameter.Value));
    }
}
