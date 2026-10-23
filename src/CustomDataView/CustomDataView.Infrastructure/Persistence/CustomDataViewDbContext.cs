using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;

namespace CustomDataView.Infrastructure.Persistence;

public sealed class CustomDataViewDbContext(DbContextOptions<CustomDataViewDbContext> options) : DbContext(options)
{
    public const string Schema = "custom_data_view";

    public DbSet<ConnectionRow> Connections => Set<ConnectionRow>();

    public DbSet<ConnectionPathParameterRow> ConnectionPathParameters => Set<ConnectionPathParameterRow>();

    public DbSet<ConnectionQueryParameterRow> ConnectionQueryParameters => Set<ConnectionQueryParameterRow>();

    public DbSet<ConnectionBodyParameterRow> ConnectionBodyParameters => Set<ConnectionBodyParameterRow>();

    public DbSet<TileRow> Tiles => Set<TileRow>();

    public DbSet<TileFieldRow> TileFields => Set<TileFieldRow>();

    public DbSet<ViewRow> Views => Set<ViewRow>();

    public DbSet<ViewTileRow> ViewTiles => Set<ViewTileRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomDataViewDbContext).Assembly);
    }
}
