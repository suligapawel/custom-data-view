using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace CustomDataView.Infrastructure.Persistence;

public sealed class CustomDataViewDbContext(DbContextOptions<CustomDataViewDbContext> options) : DbContext(options)
{
    public const string Schema = "custom_data_view";

    public const string MigrationsHistoryTable = "__ef_migrations_history";

    public DbSet<ConnectionRow> Connections => Set<ConnectionRow>();

    public DbSet<ConnectionPathParameterRow> ConnectionPathParameters => Set<ConnectionPathParameterRow>();

    public DbSet<ConnectionQueryParameterRow> ConnectionQueryParameters => Set<ConnectionQueryParameterRow>();

    public DbSet<ConnectionBodyParameterRow> ConnectionBodyParameters => Set<ConnectionBodyParameterRow>();

    public DbSet<TileRow> Tiles => Set<TileRow>();

    public DbSet<TileSourceRow> TileSources => Set<TileSourceRow>();

    public DbSet<TileFieldRow> TileFields => Set<TileFieldRow>();

    public DbSet<ViewRow> Views => Set<ViewRow>();

    public DbSet<ViewTileRow> ViewTiles => Set<ViewTileRow>();

    public static void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsql)
    {
        npgsql.MigrationsHistoryTable(MigrationsHistoryTable, Schema);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomDataViewDbContext).Assembly);
    }
}
