using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Tests.Connections;

public class ConnectionDefinitionTests
{
    [Test(Description = "scenario_TEMP-007-014 - #1")]
    public void Given_GetDefinitionWithDeclaredPathAndQueryParameters_When_Checked_Then_ItHasNoViolations()
    {
        var definition = new GetConnectionDefinition(
            "Sprzedawcy kontraktu",
            "https://api.example.com/contracts/{id}/sellers?units=metric",
            [new ParameterDeclaration("id", true, null)],
            [new ParameterDeclaration("pageSize", false, "\"20\"")]);

        Assert.That(definition.Check(), Is.Empty);
    }

    [TestCase("")]
    [TestCase("   ")]
    [Test(Description = "scenario_TEMP-007-014 - #7")]
    public void Given_BlankName_When_Checked_Then_NameIsViolated(string name)
    {
        var definition = new GetConnectionDefinition(name, "https://api.example.com", [], []);

        Assert.That(definition.Check().Keys, Is.EqualTo(new[] { "name" }));
    }

    [Test(Description = "scenario_TEMP-007-014 - #7")]
    public void Given_NameLongerThanLimit_When_Checked_Then_NameIsViolated()
    {
        var definition = new GetConnectionDefinition(new string('a', ConnectionDefinition.MaxNameLength + 1), "https://api.example.com", [], []);

        Assert.That(definition.Check().Keys, Is.EqualTo(new[] { "name" }));
    }

    [TestCase("ftp://api.example.com")]
    [TestCase("/relative/path")]
    [TestCase("https://api.example.com/{id")]
    [Test(Description = "scenario_TEMP-007-014 - #8")]
    public void Given_InvalidUrl_When_Checked_Then_UrlIsViolated(string url)
    {
        var definition = new GetConnectionDefinition("Pogoda", url, [], []);

        Assert.That(definition.Check().Keys, Is.EqualTo(new[] { "url" }));
    }

    [Test(Description = "scenario_TEMP-007-014 - #9")]
    public void Given_PlaceholderInQuery_When_Checked_Then_UrlIsViolated()
    {
        var definition = new GetConnectionDefinition("Pogoda", "https://api.example.com/weather?city={city}", [], []);

        Assert.That(definition.Check().Keys, Is.EqualTo(new[] { "url" }));
    }

    [Test(Description = "scenario_TEMP-007-014 - #13")]
    public void Given_UndeclaredUrlPlaceholderAndUnusedPathParameter_When_Checked_Then_BothAreViolated()
    {
        var definition = new GetConnectionDefinition(
            "Kontrakty",
            "https://api.example.com/contracts/{id}",
            [new ParameterDeclaration("number", true, null)],
            []);

        var violations = definition.Check();

        Assert.That(violations["url"], Is.EqualTo(new[] { "Parametr adresu \"id\" nie został zadeklarowany." }));
        Assert.That(violations["pathParameters[0].name"], Is.EqualTo(new[] { "Parametr adresu nie występuje w adresie." }));
    }

    [Test(Description = "scenario_TEMP-007-014 - #14")]
    public void Given_UndeclaredBodyPlaceholderAndUnusedBodyParameter_When_Checked_Then_BothAreViolated()
    {
        var definition = new PostConnectionDefinition(
            "Wyszukiwanie",
            "https://api.example.com/search",
            [],
            [],
            """{"limit": "{limit}"}""",
            [new ParameterDeclaration("offset", false, "0")]);

        var violations = definition.Check();

        Assert.That(violations["body"], Is.EqualTo(new[] { "Parametr treści \"limit\" nie został zadeklarowany." }));
        Assert.That(violations["bodyParameters[0].name"], Is.EqualTo(new[] { "Parametr treści nie występuje w treści." }));
    }

    [Test(Description = "scenario_TEMP-007-014 - #15")]
    public void Given_PathParameterNotInSnakeCase_When_Checked_Then_ItsNameIsViolated()
    {
        var definition = new GetConnectionDefinition(
            "Kontrakty",
            "https://api.example.com/contracts/{contractId}",
            [new ParameterDeclaration("contractId", true, null)],
            []);

        Assert.That(definition.Check().Keys, Does.Contain("pathParameters[0].name"));
    }

    [Test(Description = "scenario_TEMP-007-014 - #16")]
    public void Given_EmptyQueryParameterName_When_Checked_Then_ItsNameIsViolated()
    {
        var definition = new GetConnectionDefinition("Pogoda", "https://api.example.com", [], [new ParameterDeclaration("", false, null)]);

        Assert.That(definition.Check().Keys, Is.EqualTo(new[] { "queryParameters[0].name" }));
    }

    [Test(Description = "scenario_TEMP-007-014 - #17")]
    public void Given_RepeatedQueryParameter_When_Checked_Then_SecondNameIsViolated()
    {
        var definition = new GetConnectionDefinition(
            "Pogoda",
            "https://api.example.com",
            [],
            [new ParameterDeclaration("tag", false, null), new ParameterDeclaration("tag", false, null)]);

        Assert.That(definition.Check()["queryParameters[1].name"], Is.EqualTo(new[] { "Nazwa parametru powtarza się." }));
    }

    [Test(Description = "scenario_TEMP-007-014 - #17")]
    public void Given_PathAndBodyParametersWithSameName_When_Checked_Then_ItHasNoViolations()
    {
        var definition = new PostConnectionDefinition(
            "Kontrakty",
            "https://api.example.com/contracts/{id}",
            [new ParameterDeclaration("id", true, null)],
            [],
            """{"id": "{id}"}""",
            [new ParameterDeclaration("id", true, null)]);

        Assert.That(definition.Check(), Is.Empty);
    }

    [Test(Description = "scenario_TEMP-007-014 - #18")]
    public void Given_RequiredQueryParameterWithDefault_When_Checked_Then_DefaultValueIsViolated()
    {
        var definition = new GetConnectionDefinition("Pogoda", "https://api.example.com", [], [new ParameterDeclaration("units", true, "\"metric\"")]);

        Assert.That(definition.Check()["queryParameters[0].defaultValue"], Is.EqualTo(new[] { "Parametr wymagany nie może mieć wartości domyślnej." }));
    }

    [TestCase(null)]
    [TestCase("\"\"")]
    [TestCase("5")]
    [Test(Description = "scenario_TEMP-007-014 - #18")]
    public void Given_OptionalPathParameterWithoutNonEmptyTextDefault_When_Checked_Then_DefaultValueIsViolated(string defaultValue)
    {
        var definition = new GetConnectionDefinition(
            "Kontrakty",
            "https://api.example.com/contracts/{id}",
            [new ParameterDeclaration("id", false, defaultValue)],
            []);

        Assert.That(definition.Check().Keys, Is.EqualTo(new[] { "pathParameters[0].defaultValue" }));
    }

    [Test(Description = "scenario_TEMP-007-014 - #43")]
    public void Given_QueryParameterWithNumberDefault_When_Checked_Then_DefaultValueMustBeText()
    {
        var definition = new GetConnectionDefinition("Pogoda", "https://api.example.com", [], [new ParameterDeclaration("page", false, "1")]);

        Assert.That(definition.Check()["queryParameters[0].defaultValue"], Is.EqualTo(new[] { "Wartość domyślna parametru zapytania musi być tekstem." }));
    }

    [Test(Description = "scenario_TEMP-007-014 - #1")]
    public void Given_PostDefinition_When_ParametersAreRead_Then_PathQueryAndBodyParametersAreDeclaredAndMethodIsPost()
    {
        var definition = new PostConnectionDefinition(
            "Wyszukiwanie",
            "https://api.example.com/{id}",
            [new ParameterDeclaration("id", true, null)],
            [new ParameterDeclaration("units", false, null)],
            """{"limit": "{limit}"}""",
            [new ParameterDeclaration("limit", false, "5")]);

        Assert.That(definition.Method, Is.EqualTo(ConnectionMethods.Post));
        Assert.That(definition.DeclaredNames, Is.EquivalentTo(new[] { "id", "units", "limit" }));
    }
}
