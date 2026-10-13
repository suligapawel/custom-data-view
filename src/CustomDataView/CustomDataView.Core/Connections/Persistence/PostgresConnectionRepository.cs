using System.Data;
using System.Text.Encodings.Web;
using System.Text.Json;
using CustomDataView.Core.Connections.Definitions;
using Dapper;
using Npgsql;

namespace CustomDataView.Core.Connections.Persistence;

public sealed class PostgresConnectionRepository(NpgsqlDataSource dataSource) : IConnectionRepository
{
    private const string SelectConnections =
        "SELECT id, name, method, url, body::text AS body FROM custom_data_view.connections";

    private const string SelectPathParameters =
        "SELECT connection_id, name, required, default_value FROM custom_data_view.connection_path_parameters";

    private const string SelectQueryParameters =
        "SELECT connection_id, name, required, default_value FROM custom_data_view.connection_query_parameters";

    private const string SelectBodyParameters =
        "SELECT connection_id, name, required, default_value::text AS default_value FROM custom_data_view.connection_body_parameters";

    private static readonly JsonSerializerOptions TextValueOptions =
        new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    static PostgresConnectionRepository()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public void Add(Connection connection)
    {
        using var db = dataSource.OpenConnection();
        using var transaction = db.BeginTransaction();
        db.Execute(
            """
            INSERT INTO custom_data_view.connections (id, name, method, url, body)
            VALUES (@Id, @Name, @Method, @Url, CAST(@Body AS json))
            """,
            ToConnectionRow(connection),
            transaction);
        InsertParameters(db, transaction, connection);
        transaction.Commit();
    }

    public Connection Get(Guid id)
    {
        using var db = dataSource.OpenConnection();
        var connection = db.QuerySingleOrDefault<ConnectionRow>($"{SelectConnections} WHERE id = @Id", new { Id = id });
        if (connection is null)
        {
            return null;
        }

        var parameters = ReadParameters(db, "WHERE connection_id = @Id", new { Id = id });
        return ToConnection(connection, parameters);
    }

    public IReadOnlyList<Connection> GetAll()
    {
        using var db = dataSource.OpenConnection();
        var connections = db.Query<ConnectionRow>($"{SelectConnections} ORDER BY created_at, id");
        var parameters = ReadParameters(db, string.Empty, null);
        return connections.Select(connection => ToConnection(connection, parameters)).ToList();
    }

    public void Update(Connection connection)
    {
        using var db = dataSource.OpenConnection();
        using var transaction = db.BeginTransaction();
        db.Execute(
            """
            UPDATE custom_data_view.connections
            SET name = @Name, method = @Method, url = @Url, body = CAST(@Body AS json), updated_at = now()
            WHERE id = @Id
            """,
            ToConnectionRow(connection),
            transaction);
        db.Execute(
            """
            DELETE FROM custom_data_view.connection_path_parameters WHERE connection_id = @Id;
            DELETE FROM custom_data_view.connection_query_parameters WHERE connection_id = @Id;
            DELETE FROM custom_data_view.connection_body_parameters WHERE connection_id = @Id;
            """,
            new { connection.Id },
            transaction);
        InsertParameters(db, transaction, connection);
        transaction.Commit();
    }

    public void Delete(Guid id)
    {
        using var db = dataSource.OpenConnection();
        db.Execute("DELETE FROM custom_data_view.connections WHERE id = @Id", new { Id = id });
    }

    private static void InsertParameters(IDbConnection db, IDbTransaction transaction, Connection connection)
    {
        var definition = connection.Definition;
        db.Execute(
            """
            INSERT INTO custom_data_view.connection_path_parameters (connection_id, name, required, default_value)
            VALUES (@ConnectionId, @Name, @Required, @DefaultValue)
            """,
            ToTextParameterRows(connection.Id, definition.PathParameters),
            transaction);
        db.Execute(
            """
            INSERT INTO custom_data_view.connection_query_parameters (connection_id, name, required, default_value)
            VALUES (@ConnectionId, @Name, @Required, @DefaultValue)
            """,
            ToTextParameterRows(connection.Id, definition.QueryParameters),
            transaction);
        db.Execute(
            """
            INSERT INTO custom_data_view.connection_body_parameters (connection_id, name, required, default_value)
            VALUES (@ConnectionId, @Name, @Required, CAST(@DefaultValue AS json))
            """,
            ToParameterRows(connection.Id, (definition as PostConnectionDefinition)?.BodyParameters ?? []),
            transaction);
    }

    private static StoredParameters ReadParameters(IDbConnection db, string filter, object arguments)
    {
        using var reader = db.QueryMultiple(
            $"""
             {SelectPathParameters} {filter} ORDER BY name;
             {SelectQueryParameters} {filter} ORDER BY name;
             {SelectBodyParameters} {filter} ORDER BY name;
             """,
            arguments);
        return new StoredParameters(
            reader.Read<ParameterRow>().ToLookup(row => row.ConnectionId),
            reader.Read<ParameterRow>().ToLookup(row => row.ConnectionId),
            reader.Read<ParameterRow>().ToLookup(row => row.ConnectionId));
    }

    private static ConnectionRow ToConnectionRow(Connection connection)
    {
        var definition = connection.Definition;
        return new ConnectionRow
        {
            Id = connection.Id,
            Name = definition.Name,
            Method = ConnectionMethods.Of(definition),
            Url = definition.Url,
            Body = (definition as PostConnectionDefinition)?.Body
        };
    }

    private static Connection ToConnection(ConnectionRow row, StoredParameters parameters)
    {
        var pathParameters = ToTextDeclarations(parameters.Path[row.Id]);
        var queryParameters = ToTextDeclarations(parameters.Query[row.Id]);
        ConnectionDefinition definition = ConnectionMethods.IsGet(row.Method)
            ? new GetConnectionDefinition(row.Name, row.Url, pathParameters, queryParameters)
            : new PostConnectionDefinition(
                row.Name,
                row.Url,
                pathParameters,
                queryParameters,
                row.Body,
                ToDeclarations(parameters.Body[row.Id]));
        return new Connection(row.Id, definition);
    }

    private static List<ParameterRow> ToParameterRows(Guid connectionId, IReadOnlyList<ParameterDeclaration> parameters)
        => parameters
            .Select(parameter => new ParameterRow
            {
                ConnectionId = connectionId,
                Name = parameter.Name,
                Required = parameter.Required,
                DefaultValue = parameter.DefaultValue
            })
            .ToList();

    private static List<ParameterRow> ToTextParameterRows(Guid connectionId, IReadOnlyList<ParameterDeclaration> parameters)
        => ToParameterRows(connectionId, parameters)
            .Select(row => row with
            {
                DefaultValue = row.DefaultValue is null ? null : JsonSerializer.Deserialize<string>(row.DefaultValue)
            })
            .ToList();

    private static List<ParameterDeclaration> ToDeclarations(IEnumerable<ParameterRow> rows)
        => rows.Select(row => new ParameterDeclaration(row.Name, row.Required, row.DefaultValue)).ToList();

    private static List<ParameterDeclaration> ToTextDeclarations(IEnumerable<ParameterRow> rows)
        => rows
            .Select(row => new ParameterDeclaration(
                row.Name,
                row.Required,
                row.DefaultValue is null ? null : JsonSerializer.Serialize(row.DefaultValue, TextValueOptions)))
            .ToList();

    private sealed record StoredParameters(
        ILookup<Guid, ParameterRow> Path,
        ILookup<Guid, ParameterRow> Query,
        ILookup<Guid, ParameterRow> Body);

    private sealed record ConnectionRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public string Method { get; init; }
        public string Url { get; init; }
        public string Body { get; init; }
    }

    private sealed record ParameterRow
    {
        public Guid ConnectionId { get; init; }
        public string Name { get; init; }
        public bool Required { get; init; }
        public string DefaultValue { get; init; }
    }
}
