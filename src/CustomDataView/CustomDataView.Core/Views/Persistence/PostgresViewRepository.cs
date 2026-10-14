using CustomDataView.Core.Views.Definitions;
using Dapper;
using Npgsql;

namespace CustomDataView.Core.Views.Persistence;

public sealed class PostgresViewRepository(NpgsqlDataSource dataSource) : IViewRepository
{
    static PostgresViewRepository()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public void Add(View view)
    {
        using var db = dataSource.OpenConnection();
        using var transaction = db.BeginTransaction();
        db.Execute(
            "INSERT INTO custom_data_view.views (id, name) VALUES (@Id, @Name)",
            new { view.Id, view.Definition.Name },
            transaction);
        db.Execute(
            """
            INSERT INTO custom_data_view.view_tiles (view_id, tile_id, position, css_class)
            VALUES (@ViewId, @TileId, @Position, @CssClass)
            """,
            view.Definition.Tiles.Select(tile => new ViewTileRow
            {
                ViewId = view.Id,
                TileId = tile.TileId,
                Position = tile.Position,
                CssClass = tile.CssClass
            }),
            transaction);
        transaction.Commit();
    }

    public View Get(Guid id)
    {
        using var db = dataSource.OpenConnection();
        var view = db.QuerySingleOrDefault<ViewRow>("SELECT id, name FROM custom_data_view.views WHERE id = @Id", new { Id = id });
        if (view is null)
        {
            return null;
        }

        var tiles = db.Query<ViewTileRow>(
            """
            SELECT view_id, tile_id, position, css_class FROM custom_data_view.view_tiles
            WHERE view_id = @Id ORDER BY position
            """,
            new { Id = id });
        return new View(
            view.Id,
            new ViewDefinition(view.Name, tiles.Select(tile => new ViewTile(tile.TileId, tile.Position, tile.CssClass)).ToList()));
    }

    private sealed record ViewRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
    }

    private sealed record ViewTileRow
    {
        public Guid ViewId { get; init; }
        public Guid TileId { get; init; }
        public int Position { get; init; }
        public string CssClass { get; init; }
    }
}
