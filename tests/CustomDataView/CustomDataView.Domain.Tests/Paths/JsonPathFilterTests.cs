using System.Text.Json.Nodes;
using CustomDataView.Domain.Services;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Tests.Paths;

public class JsonPathFilterTests
{
    private const string Cities = """
                                  {"cities": [
                                    {"id": 1, "name": "Kraków", "example": {"_type": "capital"}},
                                    {"id": 2, "name": "Gdańsk", "example": {"_type": "port"}},
                                    {"id": 3, "name": "Sopot", "example": null}
                                  ]}
                                  """;

    [TestCase("$.cities[?@.example._type == 'port'].name", new[] { "Gdańsk" })]
    [TestCase("$.cities[?(@.example._type == \"port\")].name", new[] { "Gdańsk" })]
    [TestCase("$.cities[?@['example']['_type']=='capital'].name", new[] { "Kraków" })]
    [TestCase("$.cities[? ( @.example._type == 'none' ) ].name", new string[0])]
    [Test(Description = "scenario_TEMP-040 - #1")]
    public void Given_PathWithTextFilter_When_Evaluated_Then_OnlyMatchingElementsAreReturned(string text, string[] expected)
    {
        var path = Parse(text);

        var matches = path.Evaluate(JsonNode.Parse(Cities));

        Assert.That(matches.Select(match => match.GetValue<string>()), Is.EqualTo(expected));
    }

    [Test(Description = "scenario_TEMP-040 - #2")]
    public void Given_NotEqualFilter_When_ElementHasNoValue_Then_ElementMatches()
    {
        var path = Parse("$.cities[?@.example._type != 'capital'].id");

        var matches = path.Evaluate(JsonNode.Parse(Cities));

        Assert.That(matches.Select(match => match.GetValue<int>()), Is.EqualTo(new[] { 2, 3 }));
    }

    [Test(Description = "scenario_TEMP-040 - #2")]
    public void Given_PathWithFilter_When_Inspected_Then_ItReturnsCollection()
    {
        var path = Parse("$.cities[?@.id == 1]");

        Assert.That(path.IsCollection, Is.True);
    }

    [TestCase("$.cities[?@.id == 2].name", new[] { "Gdańsk" })]
    [TestCase("$.cities[?@.id == 2.0].name", new[] { "Gdańsk" })]
    [TestCase("$.cities[?@.id == 2e0].name", new[] { "Gdańsk" })]
    [TestCase("$.cities[?@.id == -2].name", new string[0])]
    [TestCase("$.cities[?@.id == '2'].name", new string[0])]
    [TestCase("$.cities[?@.id != 1].name", new[] { "Gdańsk", "Sopot" })]
    [TestCase("$.cities[?@.example == null].name", new[] { "Sopot" })]
    [TestCase("$.cities[?@.missing == null].name", new string[0])]
    [TestCase("$.cities[?@.missing == true].name", new string[0])]
    [TestCase("$.cities[?@.example._type == true].name", new string[0])]
    [Test(Description = "scenario_TEMP-040 - #3")]
    public void Given_FilterWithLiteral_When_Evaluated_Then_ValuesAreComparedByType(string text, string[] expected)
    {
        var path = Parse(text);

        var matches = path.Evaluate(JsonNode.Parse(Cities));

        Assert.That(matches.Select(match => match.GetValue<string>()), Is.EqualTo(expected));
    }

    [TestCase("""{"flags": [true, false, true]}""", "$.flags[?@ == true]", 2)]
    [TestCase("""{"tags": ["a", "b", "a"]}""", "$.tags[?@ != 'a']", 1)]
    [TestCase("""{"rows": [[1, 2], [3, 4]]}""", "$.rows[?@[-1] == 4]", 1)]
    [TestCase("""{"byCode": {"pl": {"active": true}, "de": {"active": false}}}""", "$.byCode[?@.active == true]", 1)]
    [TestCase("""{"city": {"id": 1}}""", "$.city[?@.id == 1]", 0)]
    [Test(Description = "scenario_TEMP-040 - #3")]
    public void Given_FilterOnElementsOrObjectValues_When_Evaluated_Then_MatchingCountIsReturned(string json, string text, int expected)
    {
        var path = Parse(text);

        var matches = path.Evaluate(JsonNode.Parse(json));

        Assert.That(matches, Has.Count.EqualTo(expected));
    }

    [TestCase("$.cities[?@.id]")]
    [TestCase("$.cities[?(@.x)]")]
    [TestCase("$.cities[?@.id > 1]")]
    [TestCase("$.cities[?@.id = 1]")]
    [TestCase("$.cities[?@.name == 'Gda]")]
    [TestCase("$.cities[?@.name == Gdańsk]")]
    [TestCase("$.cities[?@.id == 1.]")]
    [TestCase("$.cities[?@.id == 01]")]
    [TestCase("$.cities[?@.* == 1]")]
    [TestCase("$.cities[?@[*] == 1]")]
    [TestCase("$.cities[?@.a[?@.b == 1] == 1]")]
    [TestCase("$.cities[?(@.id == 1]")]
    [TestCase("$.cities[?@.id == 1)]")]
    [TestCase("$.cities[?$.id == 1]")]
    [TestCase("$.cities[?@.id == 1 && @.id == 2]")]
    [TestCase("$.cities[?]")]
    [TestCase("$.cities[?@.id == 1")]
    [Test(Description = "scenario_TEMP-040 - #4")]
    public void Given_InvalidFilter_When_Parsed_Then_PathIsRejected(string text)
    {
        var parsed = JsonPathParser.TryParse(text, out _);

        Assert.That(parsed, Is.False);
    }

    private static JsonPath Parse(string text)
    {
        Assert.That(JsonPathParser.TryParse(text, out var path), Is.True, text);
        return path;
    }
}
