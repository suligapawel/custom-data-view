using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class ViewTileRowConfiguration : IEntityTypeConfiguration<ViewTileRow>
{
    public void Configure(EntityTypeBuilder<ViewTileRow> builder)
    {
        builder.ToTable("view_tiles", table => table.HasCheckConstraint("ck_view_tiles_position", "position >= 0"));
        builder.HasKey(tile => new { tile.ViewId, tile.TileId }).HasName("pk_view_tiles");
        builder.Property(tile => tile.ViewId).HasColumnName("view_id");
        builder.Property(tile => tile.TileId).HasColumnName("tile_id");
        builder.Property(tile => tile.Position).HasColumnName("position");
        builder.Property(tile => tile.CssClass).HasColumnName("css_class").HasMaxLength(200);
        builder.HasAlternateKey(tile => new { tile.ViewId, tile.Position }).HasName("uq_view_tiles_view_id_position");
        builder.HasIndex(tile => tile.TileId).HasDatabaseName("ix_view_tiles_tile_id");
        builder.HasOne<TileRow>()
            .WithMany()
            .HasForeignKey(tile => tile.TileId)
            .HasConstraintName("fk_view_tiles_tile_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
