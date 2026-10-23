using System.Text.Json.Nodes;
using CustomDataView.Domain.Exceptions;
using CustomDataView.Domain.Services.Combinations;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Tests.Combinations;

public class CombinationTests
{
    private const string Dir = "scenario_TEMP-044";

    [Test(Description = $"{Dir} - #4")]
    public void Given_JoinWithoutSeparator_When_ValuesAreNumbersAndBooleans_Then_TheyAreJoinedInInvariantCulture()
    {
        var combination = new JoinCombination();

        var result = combination.Combine(null, [Input("A", JsonValue.Create(1.50m)), Input("B", JsonValue.Create(true)), Input("C", JsonValue.Create("x"))]);

        Assert.That(result!.GetValue<string>(), Is.EqualTo("1.5truex"));
    }

    [Test(Description = $"{Dir} - #5")]
    public void Given_JoinOfListWithObject_When_Combined_Then_CombinationFailsWithUnsupportedValue()
    {
        var combination = new JoinCombination();

        var failure = Assert.Throws<CombinationFailedException>(() =>
            combination.Combine(", ", [Input("Tagi", JsonNode.Parse("""["a", {"b": 1}]"""))]));

        Assert.That((failure!.Key, failure.Reason), Is.EqualTo(("join", "unsupported_value")));
    }

    [TestCase("{Ulica} {ulica }, {Miasto}", new[] { "Ulica", "Miasto" })]
    [TestCase("{{Ulica}}", new string[0])]
    [Test(Description = $"{Dir} - #3")]
    public void Given_TemplatePattern_When_InputsAreRead_Then_DistinctLabelsInOrderAreReturned(string pattern, string[] labels)
    {
        var combination = new TemplateCombination();

        var inputs = combination.Inputs(pattern, []);

        Assert.That(inputs, Is.EqualTo(labels));
    }

    [TestCase("{Ulica} {Nr")]
    [TestCase("Ulica}")]
    [TestCase("{}")]
    [TestCase("{ {Ulica}")]
    [TestCase("Adres")]
    [TestCase(null)]
    [Test(Description = $"{Dir} - #11")]
    public void Given_InvalidTemplatePattern_When_Validated_Then_ParameterError(string pattern)
    {
        var combination = new TemplateCombination();

        var errors = combination.Validate(pattern, []);

        Assert.That(errors.Select(error => error.Target), Is.EqualTo(new[] { CombinationError.ParameterTarget }));
    }

    [Test(Description = $"{Dir} - #20")]
    public void Given_ObjectOfMissingValueAndList_When_Combined_Then_MembersKeepValuesUnderInputLabels()
    {
        var combination = new ObjectCombination();

        var result = combination.Combine(null, [Input("Email", null), Input("Tagi", JsonNode.Parse("""["a", null]"""))]);

        Assert.That(JsonNode.DeepEquals(result, JsonNode.Parse("""{"Email": null, "Tagi": ["a", null]}""")), Is.True, result!.ToJsonString());
    }

    [Test(Description = $"{Dir} - #11")]
    public void Given_ObjectWithLabelsDifferingOnlyInCase_When_Validated_Then_FieldsError()
    {
        var combination = new ObjectCombination();

        var errors = combination.Validate(null, ["Lat", " lat"]);

        Assert.That(errors.Select(error => error.Target), Is.EqualTo(new[] { CombinationError.FieldsTarget }));
    }

    [Test(Description = $"{Dir} - #7")]
    public void Given_CoalesceWithEmptyTextAndValue_When_Combined_Then_EmptyTextIsFirstValue()
    {
        var combination = new CoalesceCombination();

        var result = combination.Combine(null, [Input("A", null), Input("B", JsonValue.Create("")), Input("C", JsonValue.Create("x"))]);

        Assert.That(result!.GetValue<string>(), Is.EqualTo(string.Empty));
    }

    private static CombinationInput Input(string label, JsonNode value) => new(label, value);
}
