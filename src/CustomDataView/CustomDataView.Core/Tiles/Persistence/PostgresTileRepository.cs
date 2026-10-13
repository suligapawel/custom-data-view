using CustomDataView.Core.Tiles.Definitions;
using Dapper;
using Npgsql;

namespace CustomDataView.Core.Tiles.Persistence;

public sealed class PostgresTileRepository(NpgsqlDataSource dataSource) : ITileRepository
{
    public void Add(Tile tile)
    {
        using var db = dataSource.OpenConnection();
        db.Execute(
            "INSERT INTO custom_data_view.tiles (id, name) VALUES (@Id, @Name)",
            new { tile.Id, tile.Definition.Name });
    }

    public IReadOnlyList<Tile> GetAll()
    {
        using var db = dataSource.OpenConnection();
        return db.Query<TileRow>("SELECT id, name FROM custom_data_view.tiles ORDER BY created_at, id")
            .Select(row => new Tile(row.Id, new TileDefinition(row.Name)))
            .ToList();
    }

    private sealed record TileRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
    }
}
