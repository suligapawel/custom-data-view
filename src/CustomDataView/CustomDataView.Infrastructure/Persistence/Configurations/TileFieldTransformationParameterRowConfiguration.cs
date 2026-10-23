using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class TileFieldTransformationParameterRowConfiguration
    : IEntityTypeConfiguration<TileFieldTransformationParameterRow>
{
    public void Configure(EntityTypeBuilder<TileFieldTransformationParameterRow> builder)
    {
        builder.ToTable("tile_field_transformation_parameters");
        builder.HasKey(parameter => new
            {
                parameter.TileId,
                parameter.FieldPosition,
                parameter.TransformationPosition,
                parameter.Name
            })
            .HasName("pk_tile_field_transformation_parameters");
        builder.Property(parameter => parameter.TileId).HasColumnName("tile_id");
        builder.Property(parameter => parameter.FieldPosition).HasColumnName("field_position");
        builder.Property(parameter => parameter.TransformationPosition).HasColumnName("transformation_position");
        builder.Property(parameter => parameter.Name).HasColumnName("name").HasMaxLength(50);
        builder.Property(parameter => parameter.Value).HasColumnName("value").HasColumnType("text").IsRequired();
    }
}
