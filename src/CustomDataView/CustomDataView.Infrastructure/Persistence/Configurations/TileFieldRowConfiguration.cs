using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class TileFieldRowConfiguration : IEntityTypeConfiguration<TileFieldRow>
{
    public void Configure(EntityTypeBuilder<TileFieldRow> builder)
    {
        builder.ToTable("tile_fields", table => table.HasCheckConstraint("ck_tile_fields_position", "position >= 0"));
        builder.HasKey(field => new { field.TileId, field.Position }).HasName("pk_tile_fields");
        builder.Property(field => field.TileId).HasColumnName("tile_id");
        builder.Property(field => field.Position).HasColumnName("position").ValueGeneratedNever();
        builder.Property(field => field.SourceKey).HasColumnName("source_key").HasMaxLength(50).IsRequired();
        builder.Property(field => field.Path).HasColumnName("path").HasColumnType("text").IsRequired();
        builder.Property(field => field.Label).HasColumnName("label").HasMaxLength(100).IsRequired();
        builder.Property(field => field.NormalizedLabel)
            .HasColumnName("normalized_label")
            .HasMaxLength(100)
            .IsRequired()
            .HasComputedColumnSql("lower(btrim(label))", stored: true);
        builder.Property(field => field.Hidden).HasColumnName("hidden").HasDefaultValue(false);
        builder.HasIndex(field => new { field.TileId, field.NormalizedLabel })
            .IsUnique()
            .HasDatabaseName("ux_tile_fields_tile_id_normalized_label");
        builder.HasIndex(field => new { field.TileId, field.SourceKey }).HasDatabaseName("ix_tile_fields_tile_id_source_key");
        builder.HasOne<TileSourceRow>()
            .WithMany()
            .HasForeignKey(field => new { field.TileId, field.SourceKey })
            .HasConstraintName("fk_tile_fields_source")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(field => field.Transformations)
            .WithOne()
            .HasForeignKey(transformation => new { transformation.TileId, transformation.FieldPosition })
            .HasConstraintName("fk_tile_field_transformations_field")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
