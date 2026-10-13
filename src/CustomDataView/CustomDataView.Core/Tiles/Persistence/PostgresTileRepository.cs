using CustomDataView.Core.Tiles.Definitions;
using Dapper;
using Npgsql;

namespace CustomDataView.Core.Tiles.Persistence;

public sealed class PostgresTileRepository(NpgsqlDataSource dataSource) : ITileRepository
{
    private const string SelectFields =
        "SELECT tile_id, position, connection_id, path, label FROM custom_data_view.tile_fields";

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
        db.Execute(
            """
            INSERT INTO custom_data_view.tile_fields (tile_id, position, connection_id, path, label)
            VALUES (@TileId, @Position, @ConnectionId, @Path, @Label)
            """,
            tile.Definition.Fields.Select((field, position) => new FieldRow
            {
                TileId = tile.Id,
                Position = position,
                ConnectionId = field.ConnectionId,
                Path = field.Path,
                Label = field.Label
            }),
            transaction);
        transaction.Commit();
    }

    public Tile Get(Guid id)
    {
        using var db = dataSource.OpenConnection();
        var tile = db.QuerySingleOrDefault<TileRow>("SELECT id, name FROM custom_data_view.tiles WHERE id = @Id", new { Id = id });
        if (tile is null)
        {
            return null;
        }

        var fields = db.Query<FieldRow>($"{SelectFields} WHERE tile_id = @Id ORDER BY position", new { Id = id });
        return ToTile(tile, fields.ToLookup(field => field.TileId));
    }

    public IReadOnlyList<Tile> GetAll()
    {
        using var db = dataSource.OpenConnection();
        var tiles = db.Query<TileRow>("SELECT id, name FROM custom_data_view.tiles ORDER BY created_at, id");
        var fields = db.Query<FieldRow>($"{SelectFields} ORDER BY tile_id, position").ToLookup(field => field.TileId);
        return tiles.Select(tile => ToTile(tile, fields)).ToList();
    }

    private static Tile ToTile(TileRow tile, ILookup<Guid, FieldRow> fields)
        => new Tile(tile.Id, new TileDefinition(
            tile.Name,
            fields[tile.Id].Select(field => new TileField(field.ConnectionId, field.Path, field.Label)).ToList()));

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
}
