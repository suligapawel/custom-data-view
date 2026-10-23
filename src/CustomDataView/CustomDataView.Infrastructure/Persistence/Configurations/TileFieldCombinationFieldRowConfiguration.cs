using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class TileFieldCombinationFieldRowConfiguration : IEntityTypeConfiguration<TileFieldCombinationFieldRow>
{
    public void Configure(EntityTypeBuilder<TileFieldCombinationFieldRow> builder)
    {
        builder.ToTable(
            "tile_field_combination_fields",
            table => table.HasCheckConstraint("ck_tile_field_combination_fields_position", "position >= 0"));
        builder.HasKey(field => new { field.TileId, field.FieldLabel, field.Position }).HasName("pk_tile_field_combination_fields");
        builder.Property(field => field.TileId).HasColumnName("tile_id");
        builder.Property(field => field.FieldLabel).HasColumnName("field_label").HasMaxLength(100);
        builder.Property(field => field.Position).HasColumnName("position").ValueGeneratedNever();
        builder.Property(field => field.InputLabel).HasColumnName("input_label").HasMaxLength(100).IsRequired();
    }
}
