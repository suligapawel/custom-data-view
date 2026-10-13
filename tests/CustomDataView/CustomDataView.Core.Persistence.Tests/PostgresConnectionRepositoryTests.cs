using System.Text.Json;
using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Persistence;
using Dapper;
using Npgsql;

namespace CustomDataView.Core.Persistence.Tests;

public class PostgresConnectionRepositoryTests
{
    private NpgsqlDataSource dataSource;
    private PostgresConnectionRepository repository;

    [OneTimeSetUp]
    public void CreateDatabase()
    {
        dataSource = TestDatabase.Create();
        repository = new PostgresConnectionRepository(dataSource);
    }

    [OneTimeTearDown]
    public void DisposeDatabase()
    {
        dataSource?.Dispose();
    }

    [SetUp]
    public void ClearDatabase()
    {
        TestDatabase.Clear(dataSource);
    }

    [Test]
    public void Given_SavedGetConnection_When_ItIsLoaded_Then_DefinitionIsUnchanged()
    {
        var connection = new Connection(Guid.NewGuid(), new GetConnectionDefinition(
            "Sprzedawcy kontraktu",
            "https://api.example.com/contracts/{id}/sellers?units=metric",
            [new ParameterDeclaration("id", true, null)],
            [new ParameterDeclaration("pageSize", false, "\"20\""), new ParameterDeclaration("q", false, "\"żółw\"")]));

        repository.Add(connection);

        AssertSame(repository.Get(connection.Id), connection);
    }

    [Test]
    public void Given_SavedPostConnection_When_ItIsLoaded_Then_BodyTemplateAndParametersAreUnchanged()
    {
        var connection = new Connection(Guid.NewGuid(), new PostConnectionDefinition(
            "Wyszukiwanie sprzedawców",
            "https://api.example.com/sellers/search",
            [],
            [],
            """{"limit": "{limit}",  "contractId": "{id}"}""",
            [
                new ParameterDeclaration("filter", false, """{"status": "active"}"""),
                new ParameterDeclaration("id", true, null),
                new ParameterDeclaration("limit", false, "10")
            ]));

        repository.Add(connection);

        AssertSame(repository.Get(connection.Id), connection);
    }

    [Test]
    public void Given_PostConnectionWithoutBody_When_ItIsLoaded_Then_BodyIsEmpty()
    {
        var connection = new Connection(
            Guid.NewGuid(),
            new PostConnectionDefinition("Odświeżenie", "https://api.example.com/refresh", [], [], null, []));

        repository.Add(connection);

        AssertSame(repository.Get(connection.Id), connection);
    }

    [Test]
    public void Given_ParametersDeclaredInAnyOrder_When_ConnectionIsLoaded_Then_ParametersAreSortedByName()
    {
        var connection = new Connection(Guid.NewGuid(), new GetConnectionDefinition(
            "Sprzedawcy",
            "https://api.example.com/sellers",
            [],
            [new ParameterDeclaration("tag", false, null), new ParameterDeclaration("page", false, null)]));

        repository.Add(connection);

        Assert.That(
            repository.Get(connection.Id).Definition.QueryParameters.Select(parameter => parameter.Name),
            Is.EqualTo(new[] { "page", "tag" }));
    }

    [Test]
    public void Given_UnknownId_When_ConnectionIsLoaded_Then_NothingIsReturned()
    {
        Assert.That(repository.Get(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public void Given_TwoSavedConnections_When_AllAreLoaded_Then_TheyAreInAddingOrderWithParameters()
    {
        var first = new Connection(Guid.NewGuid(), new GetConnectionDefinition(
            "Pierwsze",
            "https://api.example.com/{id}",
            [new ParameterDeclaration("id", true, null)],
            []));
        var second = new Connection(Guid.NewGuid(), new PostConnectionDefinition(
            "Drugie",
            "https://api.example.com/search",
            [],
            [],
            """{"id": "{id}"}""",
            [new ParameterDeclaration("id", true, null)]));

        repository.Add(first);
        repository.Add(second);

        var connections = repository.GetAll();
        Assert.That(connections, Has.Count.EqualTo(2));
        AssertSame(connections[0], first);
        AssertSame(connections[1], second);
    }

    [Test]
    public void Given_SavedGetConnection_When_ItIsReplacedWithPostDefinition_Then_NewDefinitionIsLoadedAndPositionIsKept()
    {
        var first = new Connection(Guid.NewGuid(), new GetConnectionDefinition(
            "Pierwsze",
            "https://api.example.com/{id}",
            [new ParameterDeclaration("id", true, null)],
            [new ParameterDeclaration("page", false, "\"1\"")]));
        var second = new Connection(
            Guid.NewGuid(),
            new GetConnectionDefinition("Drugie", "https://api.example.com/other", [], []));
        repository.Add(first);
        repository.Add(second);
        var updated = first with
        {
            Definition = new PostConnectionDefinition(
                "Pierwsze po edycji",
                "https://api.example.com/search",
                [],
                [new ParameterDeclaration("units", false, "\"metric\"")],
                """{"limit": "{limit}"}""",
                [new ParameterDeclaration("limit", false, "5")])
        };

        repository.Update(updated);

        var connections = repository.GetAll();
        AssertSame(connections[0], updated);
        AssertSame(connections[1], second);
    }

    [Test]
    public void Given_SavedConnection_When_ItIsDeleted_Then_ConnectionAndParametersAreRemoved()
    {
        var connection = new Connection(Guid.NewGuid(), new PostConnectionDefinition(
            "Wyszukiwanie",
            "https://api.example.com/{id}/search",
            [new ParameterDeclaration("id", true, null)],
            [new ParameterDeclaration("page", false, null)],
            """{"limit": "{limit}"}""",
            [new ParameterDeclaration("limit", true, null)]));
        repository.Add(connection);

        repository.Delete(connection.Id);

        Assert.That(repository.Get(connection.Id), Is.Null);
        using var db = dataSource.OpenConnection();
        Assert.That(
            db.ExecuteScalar<long>(
                """
                SELECT (SELECT count(*) FROM custom_data_view.connection_path_parameters)
                     + (SELECT count(*) FROM custom_data_view.connection_query_parameters)
                     + (SELECT count(*) FROM custom_data_view.connection_body_parameters)
                """),
            Is.Zero);
    }

    private static void AssertSame(Connection actual, Connection expected)
    {
        Assert.That(actual.Id, Is.EqualTo(expected.Id));
        Assert.That(
            JsonSerializer.Serialize<object>(actual.Definition),
            Is.EqualTo(JsonSerializer.Serialize<object>(expected.Definition)));
    }
}
