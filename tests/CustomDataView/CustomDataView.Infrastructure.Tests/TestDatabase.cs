using CustomDataView.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace CustomDataView.Infrastructure.Tests;

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
        Execute(dataSource, "DROP SCHEMA IF EXISTS custom_data_view CASCADE");
        foreach (var migration in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Migrations"), "*.sql").Order())
        {
            Execute(dataSource, File.ReadAllText(migration));
        }

        return dataSource;
    }

    public static IDbContextFactory<CustomDataViewDbContext> ContextFactory(NpgsqlDataSource dataSource)
        => new PooledDbContextFactory<CustomDataViewDbContext>(
            new DbContextOptionsBuilder<CustomDataViewDbContext>().UseNpgsql(dataSource).Options);

    public static void Clear(NpgsqlDataSource dataSource)
    {
        Execute(dataSource, "TRUNCATE custom_data_view.views, custom_data_view.tiles, custom_data_view.connections CASCADE");
    }

    private static void Execute(NpgsqlDataSource dataSource, string sql)
    {
        using var command = dataSource.CreateCommand(sql);
        command.ExecuteNonQuery();
    }
}
