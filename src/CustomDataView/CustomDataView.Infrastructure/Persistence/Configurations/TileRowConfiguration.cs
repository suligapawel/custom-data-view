using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class TileRowConfiguration : IEntityTypeConfiguration<TileRow>
{
    public void Configure(EntityTypeBuilder<TileRow> builder)
    {
        builder.ToTable("tiles", table =>
        {
            table.HasCheckConstraint("ck_tiles_display", "display IN ('table', 'list', 'details', 'summary', 'form')");
            table.HasCheckConstraint(
                "ck_tiles_items",
                "(display IN ('table', 'list')) = (items_source_key IS NOT NULL AND items_path IS NOT NULL AND items_key_label IS NOT NULL) "
                + "AND (display IN ('table', 'list') OR (items_source_key IS NULL AND items_path IS NULL AND items_key_label IS NULL))");
        });
        builder.HasKey(tile => tile.Id).HasName("pk_tiles");
        builder.Property(tile => tile.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(tile => tile.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(tile => tile.Display).HasColumnName("display").HasMaxLength(10).IsRequired();
        builder.Property(tile => tile.ItemsSourceKey).HasColumnName("items_source_key").HasMaxLength(50);
        builder.Property(tile => tile.ItemsPath).HasColumnName("items_path").HasColumnType("text");
        builder.Property(tile => tile.ItemsKeyLabel).HasColumnName("items_key_label").HasMaxLength(100);
        builder.Property(tile => tile.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
        builder.Property(tile => tile.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
        builder.HasIndex(tile => new { tile.CreatedAt, tile.Id }).HasDatabaseName("ix_tiles_created_at_id");
        builder.HasMany(tile => tile.Sources)
            .WithOne()
            .HasForeignKey(source => source.TileId)
            .HasConstraintName("fk_tile_sources_tile_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(tile => tile.Children)
            .WithOne()
            .HasForeignKey(child => child.ParentTileId)
            .HasConstraintName("fk_tile_children_parent_tile_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(tile => tile.FormFields)
            .WithOne()
            .HasForeignKey(field => field.TileId)
            .HasConstraintName("fk_tile_form_fields_tile_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(tile => tile.Bindings)
            .WithOne()
            .HasForeignKey(binding => binding.TileId)
            .HasConstraintName("fk_tile_bindings_tile_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(tile => tile.Fields)
            .WithOne()
            .HasForeignKey(field => field.TileId)
            .HasConstraintName("fk_tile_fields_tile_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
