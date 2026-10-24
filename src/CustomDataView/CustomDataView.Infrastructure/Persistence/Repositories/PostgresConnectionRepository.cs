using System.Text.Encodings.Web;
using System.Text.Json;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;
using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;

namespace CustomDataView.Infrastructure.Persistence.Repositories;

public sealed class PostgresConnectionRepository(IDbContextFactory<CustomDataViewDbContext> contextFactory)
    : IConnectionRepository
{
    private static readonly JsonSerializerOptions TextValueOptions =
        new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public void Add(Connection connection)
    {
        using var context = contextFactory.CreateDbContext();
        var row = ToConnectionRow(connection);
        AddParameters(row, connection);
        context.Connections.Add(row);
        context.SaveChanges();
    }

    public Connection Get(Guid id)
    {
        using var context = contextFactory.CreateDbContext();
        var row = WithParameters(context).SingleOrDefault(connection => connection.Id == id);
        return row is null ? null : ToConnection(row);
    }

    public IReadOnlyList<Connection> GetAll()
    {
        using var context = contextFactory.CreateDbContext();
        return WithParameters(context)
            .OrderBy(connection => connection.CreatedAt)
            .ThenBy(connection => connection.Id)
            .AsEnumerable()
            .Select(ToConnection)
            .ToList();
    }

    public void Update(Connection connection)
    {
        using var context = contextFactory.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();
        var row = ToConnectionRow(connection);
        context.Connections
            .Where(stored => stored.Id == connection.Id)
            .ExecuteUpdate(setters => setters
                .SetProperty(stored => stored.Name, row.Name)
                .SetProperty(stored => stored.Method, row.Method)
                .SetProperty(stored => stored.Url, row.Url)
                .SetProperty(stored => stored.Body, row.Body)
                .SetProperty(stored => stored.UpdatedAt, stored => DateTime.UtcNow));
        context.ConnectionPathParameters.Where(parameter => parameter.ConnectionId == connection.Id).ExecuteDelete();
        context.ConnectionQueryParameters.Where(parameter => parameter.ConnectionId == connection.Id).ExecuteDelete();
        context.ConnectionBodyParameters.Where(parameter => parameter.ConnectionId == connection.Id).ExecuteDelete();
        AddParameters(row, connection);
        context.ConnectionPathParameters.AddRange(row.PathParameters);
        context.ConnectionQueryParameters.AddRange(row.QueryParameters);
        context.ConnectionBodyParameters.AddRange(row.BodyParameters);
        context.SaveChanges();
        transaction.Commit();
    }

    public void Delete(Guid id)
    {
        using var context = contextFactory.CreateDbContext();
        context.Connections.Where(connection => connection.Id == id).ExecuteDelete();
    }

    private static IQueryable<ConnectionRow> WithParameters(CustomDataViewDbContext context)
        => context.Connections
            .AsNoTracking()
            .Include(connection => connection.PathParameters.OrderBy(parameter => parameter.Name))
            .Include(connection => connection.QueryParameters.OrderBy(parameter => parameter.Name))
            .Include(connection => connection.BodyParameters.OrderBy(parameter => parameter.Name))
            .AsSplitQuery();

    private static ConnectionRow ToConnectionRow(Connection connection)
    {
        var definition = connection.Definition;
        return new ConnectionRow
        {
            Id = connection.Id,
            Name = definition.Name,
            Method = definition.Method,
            Url = definition.Url,
            Body = (definition as PostConnectionDefinition)?.Body
        };
    }

    private static void AddParameters(ConnectionRow row, Connection connection)
    {
        var definition = connection.Definition;
        row.PathParameters = definition.PathParameters
            .Select(parameter => new ConnectionPathParameterRow
            {
                ConnectionId = connection.Id,
                Name = parameter.Name,
                Required = parameter.Required,
                DefaultValue = ToText(parameter.DefaultValue)
            })
            .ToList();
        row.QueryParameters = definition.QueryParameters
            .Select(parameter => new ConnectionQueryParameterRow
            {
                ConnectionId = connection.Id,
                Name = parameter.Name,
                Required = parameter.Required,
                DefaultValue = ToText(parameter.DefaultValue)
            })
            .ToList();
        row.BodyParameters = ((definition as PostConnectionDefinition)?.BodyParameters ?? [])
            .Select(parameter => new ConnectionBodyParameterRow
            {
                ConnectionId = connection.Id,
                Name = parameter.Name,
                Required = parameter.Required,
                DefaultValue = parameter.DefaultValue
            })
            .ToList();
    }

    private static Connection ToConnection(ConnectionRow row)
    {
        var pathParameters = row.PathParameters
            .Select(parameter => new ParameterDeclaration(parameter.Name, parameter.Required, FromText(parameter.DefaultValue)))
            .ToList();
        var queryParameters = row.QueryParameters
            .Select(parameter => new ParameterDeclaration(parameter.Name, parameter.Required, FromText(parameter.DefaultValue)))
            .ToList();
        ConnectionDefinition definition = ConnectionMethods.IsGet(row.Method)
            ? new GetConnectionDefinition(row.Name, row.Url, pathParameters, queryParameters)
            : new PostConnectionDefinition(
                row.Name,
                row.Url,
                pathParameters,
                queryParameters,
                row.Body,
                row.BodyParameters
                    .Select(parameter => new ParameterDeclaration(parameter.Name, parameter.Required, parameter.DefaultValue))
                    .ToList());
        return Connection.Restore(row.Id, definition);
    }

    private static string ToText(string defaultValue)
        => defaultValue is null ? null : JsonSerializer.Deserialize<string>(defaultValue);

    private static string FromText(string defaultValue)
        => defaultValue is null ? null : JsonSerializer.Serialize(defaultValue, TextValueOptions);
}
