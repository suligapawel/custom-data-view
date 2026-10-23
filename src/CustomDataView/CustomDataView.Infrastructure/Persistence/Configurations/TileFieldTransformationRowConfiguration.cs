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
        builder.HasKey(transformation => new { transformation.TileId, transformation.FieldPosition, transformation.Position })
            .HasName("pk_tile_field_transformations");
        builder.Property(transformation => transformation.TileId).HasColumnName("tile_id");
        builder.Property(transformation => transformation.FieldPosition).HasColumnName("field_position");
        builder.Property(transformation => transformation.Position).HasColumnName("position").ValueGeneratedNever();
        builder.Property(transformation => transformation.Key).HasColumnName("key").HasMaxLength(50).IsRequired();
        builder.HasMany(transformation => transformation.Parameters)
            .WithOne()
            .HasForeignKey(parameter => new { parameter.TileId, parameter.FieldPosition, parameter.TransformationPosition })
            .HasConstraintName("fk_tile_field_transformation_parameters_transformation")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
