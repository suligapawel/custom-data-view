using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class TileFieldTransformationRowConfiguration : IEntityTypeConfiguration<TileFieldTransformationRow>
{
    public void Configure(EntityTypeBuilder<TileFieldTransformationRow> builder)
    {
        builder.ToTable(
            "tile_field_transformations",
            table => table.HasCheckConstraint("ck_tile_field_transformations_position", "position >= 0"));
        builder.HasKey(transformation => new { transformation.TileId, transformation.FieldLabel, transformation.Position })
            .HasName("pk_tile_field_transformations");
        builder.Property(transformation => transformation.TileId).HasColumnName("tile_id");
        builder.Property(transformation => transformation.FieldLabel).HasColumnName("field_label").HasMaxLength(100);
        builder.Property(transformation => transformation.Position).HasColumnName("position").ValueGeneratedNever();
        builder.Property(transformation => transformation.Key).HasColumnName("key").HasMaxLength(50).IsRequired();
        builder.Property(transformation => transformation.Parameter).HasColumnName("parameter").HasColumnType("text");
    }
}
