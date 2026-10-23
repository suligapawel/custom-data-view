using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class TileFormFieldRowConfiguration : IEntityTypeConfiguration<TileFormFieldRow>
{
    public void Configure(EntityTypeBuilder<TileFormFieldRow> builder)
    {
        builder.ToTable("tile_form_fields", table =>
        {
            table.HasCheckConstraint("ck_tile_form_fields_position", "position >= 0");
            table.HasCheckConstraint("ck_tile_form_fields_type", "type IN ('text', 'number', 'boolean', 'date')");
        });
        builder.HasKey(field => new { field.TileId, field.Position }).HasName("pk_tile_form_fields");
        builder.Property(field => field.TileId).HasColumnName("tile_id");
        builder.Property(field => field.Position).HasColumnName("position").ValueGeneratedNever();
        builder.Property(field => field.Label).HasColumnName("label").HasMaxLength(100).IsRequired();
        builder.Property(field => field.NormalizedLabel)
            .HasColumnName("normalized_label")
            .HasMaxLength(100)
            .IsRequired()
            .HasComputedColumnSql("lower(btrim(label))", stored: true);
        builder.Property(field => field.Type).HasColumnName("type").HasMaxLength(10).IsRequired();
        builder.Property(field => field.Required).HasColumnName("required").HasDefaultValue(false);
        builder.Property(field => field.DefaultValue).HasColumnName("default_value").HasColumnType("json");
        builder.HasIndex(field => new { field.TileId, field.NormalizedLabel })
            .IsUnique()
            .HasDatabaseName("ux_tile_form_fields_tile_id_normalized_label");
    }
}
