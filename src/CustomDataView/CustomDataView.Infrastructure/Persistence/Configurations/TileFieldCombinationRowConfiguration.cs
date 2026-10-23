using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class TileFieldCombinationRowConfiguration : IEntityTypeConfiguration<TileFieldCombinationRow>
{
    public void Configure(EntityTypeBuilder<TileFieldCombinationRow> builder)
    {
        builder.ToTable("tile_field_combinations");
        builder.HasKey(combination => new { combination.TileId, combination.FieldLabel }).HasName("pk_tile_field_combinations");
        builder.Property(combination => combination.TileId).HasColumnName("tile_id");
        builder.Property(combination => combination.FieldLabel).HasColumnName("field_label").HasMaxLength(100);
        builder.Property(combination => combination.Key).HasColumnName("key").HasMaxLength(50).IsRequired();
        builder.Property(combination => combination.Parameter).HasColumnName("parameter").HasColumnType("text");
        builder.HasMany(combination => combination.Fields)
            .WithOne()
            .HasForeignKey(field => new { field.TileId, field.FieldLabel })
            .HasConstraintName("fk_tile_field_combination_fields_combination")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
