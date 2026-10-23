using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class TileSourceRowConfiguration : IEntityTypeConfiguration<TileSourceRow>
{
    public void Configure(EntityTypeBuilder<TileSourceRow> builder)
    {
        builder.ToTable("tile_sources", table =>
        {
            table.HasCheckConstraint("ck_tile_sources_kind", "kind IN ('connection', 'input')");
            table.HasCheckConstraint("ck_tile_sources_position", "position >= 0");
            table.HasCheckConstraint(
                "ck_tile_sources_connection",
                "(kind = 'connection' AND connection_id IS NOT NULL) OR (kind <> 'connection' AND connection_id IS NULL)");
        });
        builder.HasKey(source => new { source.TileId, source.Key }).HasName("pk_tile_sources");
        builder.Property(source => source.TileId).HasColumnName("tile_id");
        builder.Property(source => source.Key).HasColumnName("key").HasMaxLength(50);
        builder.Property(source => source.Position).HasColumnName("position").ValueGeneratedNever();
        builder.HasAlternateKey(source => new { source.TileId, source.Position }).HasName("uq_tile_sources_tile_id_position");
        builder.Property(source => source.Kind).HasColumnName("kind").HasMaxLength(10).IsRequired();
        builder.Property(source => source.ConnectionId).HasColumnName("connection_id");
        builder.HasIndex(source => source.ConnectionId).HasDatabaseName("ix_tile_sources_connection_id");
        builder.HasOne<ConnectionRow>()
            .WithMany()
            .HasForeignKey(source => source.ConnectionId)
            .HasConstraintName("fk_tile_sources_connection_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
