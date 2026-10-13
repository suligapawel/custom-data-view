using System.Data;
using System.Text.Json.Nodes;
using CustomDataView.Core.Tiles.Definitions;
using CustomDataView.Core.Transformations.Abstractions;
using CustomDataView.Core.Transformations.Execution;
using Dapper;
using Npgsql;

namespace CustomDataView.Core.Tiles.Persistence;

public sealed class PostgresTileRepository(NpgsqlDataSource dataSource, IEnumerable<ITransformation> transformations)
    : ITileRepository
{
    private const string SelectTiles = "SELECT id, name FROM custom_data_view.tiles";

    private const string SelectFields =
        "SELECT tile_id, position, connection_id, path, label FROM custom_data_view.tile_fields";

    private const string SelectTransformations =
        "SELECT tile_id, field_position, position, key FROM custom_data_view.tile_field_transformations";

    private const string SelectParameters =
        """
        SELECT tile_id, field_position, transformation_position, name, value
        FROM custom_data_view.tile_field_transformation_parameters
        """;

    private readonly Dictionary<string, ITransformation> catalog =
        transformations.ToDictionary(transformation => transformation.Key, StringComparer.Ordinal);

    static PostgresTileRepository()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public void Add(Tile tile)
    {
        using var db = dataSource.OpenConnection();
        using var transaction = db.BeginTransaction();
        db.Execute(
            "INSERT INTO custom_data_view.tiles (id, name) VALUES (@Id, @Name)",
            new { tile.Id, tile.Definition.Name },
            transaction);
        InsertFields(db, transaction, tile);
        transaction.Commit();
    }

    public Tile Get(Guid id)
    {
        using var db = dataSource.OpenConnection();
        var tile = db.QuerySingleOrDefault<TileRow>($"{SelectTiles} WHERE id = @Id", new { Id = id });
        return tile is null ? null : ToTile(tile, ReadFields(db, "WHERE tile_id = @Id", new { Id = id }));
    }

    public IReadOnlyList<Tile> GetAll()
    {
        using var db = dataSource.OpenConnection();
        var tiles = db.Query<TileRow>($"{SelectTiles} ORDER BY created_at, id");
        var fields = ReadFields(db, string.Empty, null);
        return tiles.Select(tile => ToTile(tile, fields)).ToList();
    }

    public void Update(Tile tile)
    {
        using var db = dataSource.OpenConnection();
        using var transaction = db.BeginTransaction();
        db.Execute(
            "UPDATE custom_data_view.tiles SET name = @Name, updated_at = now() WHERE id = @Id",
            new { tile.Id, tile.Definition.Name },
            transaction);
        db.Execute("DELETE FROM custom_data_view.tile_fields WHERE tile_id = @Id", new { tile.Id }, transaction);
        InsertFields(db, transaction, tile);
        transaction.Commit();
    }

    public void Delete(Guid id)
    {
        using var db = dataSource.OpenConnection();
        db.Execute("DELETE FROM custom_data_view.tiles WHERE id = @Id", new { Id = id });
    }

    private static void InsertFields(IDbConnection db, IDbTransaction transaction, Tile tile)
    {
        var fields = tile.Definition.Fields;
        db.Execute(
            """
            INSERT INTO custom_data_view.tile_fields (tile_id, position, connection_id, path, label)
            VALUES (@TileId, @Position, @ConnectionId, @Path, @Label)
            """,
            fields.Select((field, position) => new FieldRow
            {
                TileId = tile.Id,
                Position = position,
                ConnectionId = field.ConnectionId,
                Path = field.Path,
                Label = field.Label
            }),
            transaction);
        db.Execute(
            """
            INSERT INTO custom_data_view.tile_field_transformations (tile_id, field_position, position, key)
            VALUES (@TileId, @FieldPosition, @Position, @Key)
            """,
            fields.SelectMany((field, fieldPosition) => field.Transformations.Select((step, position) => new TransformationRow
            {
                TileId = tile.Id,
                FieldPosition = fieldPosition,
                Position = position,
                Key = step.Key
            })),
            transaction);
        db.Execute(
            """
            INSERT INTO custom_data_view.tile_field_transformation_parameters
                (tile_id, field_position, transformation_position, name, value)
            VALUES (@TileId, @FieldPosition, @TransformationPosition, @Name, @Value)
            """,
            fields.SelectMany((field, fieldPosition) => field.Transformations.SelectMany((step, position) =>
                (step.Parameters ?? new Dictionary<string, JsonNode>()).Select(parameter => new ParameterRow
                {
                    TileId = tile.Id,
                    FieldPosition = fieldPosition,
                    TransformationPosition = position,
                    Name = parameter.Key,
                    Value = TransformationParameterValues.ToText(parameter.Value)
                }))),
            transaction);
    }

    private StoredFields ReadFields(IDbConnection db, string filter, object arguments)
    {
        using var reader = db.QueryMultiple(
            $"""
             {SelectFields} {filter} ORDER BY tile_id, position;
             {SelectTransformations} {filter} ORDER BY tile_id, field_position, position;
             {SelectParameters} {filter} ORDER BY tile_id, field_position, transformation_position, name;
             """,
            arguments);
        return new StoredFields(
            reader.Read<FieldRow>().ToLookup(row => row.TileId),
            reader.Read<TransformationRow>().ToLookup(row => (row.TileId, row.FieldPosition)),
            reader.Read<ParameterRow>().ToLookup(row => (row.TileId, row.FieldPosition, row.TransformationPosition)));
    }

    private Tile ToTile(TileRow tile, StoredFields stored)
        => new Tile(tile.Id, new TileDefinition(
            tile.Name,
            stored.Fields[tile.Id]
                .Select(field => new TileField(
                    field.ConnectionId,
                    field.Path,
                    field.Label,
                    stored.Transformations[(tile.Id, field.Position)]
                        .Select(step => ToStep(step, stored.Parameters[(tile.Id, field.Position, step.Position)]))
                        .ToList()))
                .ToList()));

    private TransformationStep ToStep(TransformationRow step, IEnumerable<ParameterRow> parameters)
    {
        var transformation = catalog.GetValueOrDefault(step.Key);
        return new TransformationStep(
            step.Key,
            parameters.ToDictionary(
                parameter => parameter.Name,
                parameter => TransformationParameterValues.FromText(transformation, parameter.Name, parameter.Value)));
    }

    private sealed record StoredFields(
        ILookup<Guid, FieldRow> Fields,
        ILookup<(Guid, int), TransformationRow> Transformations,
        ILookup<(Guid, int, int), ParameterRow> Parameters);

    private sealed record TileRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
    }

    private sealed record FieldRow
    {
        public Guid TileId { get; init; }
        public int Position { get; init; }
        public Guid ConnectionId { get; init; }
        public string Path { get; init; }
        public string Label { get; init; }
    }

    private sealed record TransformationRow
    {
        public Guid TileId { get; init; }
        public int FieldPosition { get; init; }
        public int Position { get; init; }
        public string Key { get; init; }
    }

    private sealed record ParameterRow
    {
        public Guid TileId { get; init; }
        public int FieldPosition { get; init; }
        public int TransformationPosition { get; init; }
        public string Name { get; init; }
        public string Value { get; init; }
    }
}
