using Dapper;
using Npgsql;

namespace CustomDataView.Core.Persistence.Tests;

public static class TestDatabase
{
    private const string ConnectionStringVariable = "CUSTOM_DATA_VIEW_TEST_DATABASE";
    private const string TestDatabaseSuffix = "_test";

    public static NpgsqlDataSource Create()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrEmpty(connectionString))
        {
            Assert.Ignore($"Set {ConnectionStringVariable} to run PostgreSQL repository tests.");
        }

        var database = new NpgsqlConnectionStringBuilder(connectionString).Database ?? string.Empty;
        if (!database.EndsWith(TestDatabaseSuffix, StringComparison.Ordinal))
        {
            Assert.Fail($"Test database name must end with \"{TestDatabaseSuffix}\".");
        }

        var dataSource = NpgsqlDataSource.Create(connectionString);
        using var connection = dataSource.OpenConnection();
        connection.Execute("DROP SCHEMA IF EXISTS custom_data_view CASCADE");
        foreach (var migration in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Migrations"), "*.sql").Order())
        {
            connection.Execute(File.ReadAllText(migration));
        }

        return dataSource;
    }

    public static void Clear(NpgsqlDataSource dataSource)
    {
        using var connection = dataSource.OpenConnection();
        connection.Execute("TRUNCATE custom_data_view.tiles, custom_data_view.connections CASCADE");
    }
}
