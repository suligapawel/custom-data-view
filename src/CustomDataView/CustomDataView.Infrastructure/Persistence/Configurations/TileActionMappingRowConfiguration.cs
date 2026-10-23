using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class TileActionMappingRowConfiguration : IEntityTypeConfiguration<TileActionMappingRow>
{
    public void Configure(EntityTypeBuilder<TileActionMappingRow> builder)
    {
        builder.ToTable("tile_action_mappings", table =>
        {
            table.HasCheckConstraint("ck_tile_action_mappings_target_kind", "target_kind IN ('parameter', 'input')");
            table.HasCheckConstraint("ck_tile_action_mappings_position", "position >= 0");
        });
        builder.HasKey(mapping => new { mapping.TileId, mapping.ActionKey, mapping.TargetKind, mapping.TargetName })
            .HasName("pk_tile_action_mappings");
        builder.Property(mapping => mapping.TileId).HasColumnName("tile_id");
        builder.Property(mapping => mapping.ActionKey).HasColumnName("action_key").HasMaxLength(50);
        builder.Property(mapping => mapping.TargetKind).HasColumnName("target_kind").HasMaxLength(9);
        builder.Property(mapping => mapping.TargetName).HasColumnName("target_name").HasMaxLength(50);
        builder.Property(mapping => mapping.SourceField).HasColumnName("source_field").HasMaxLength(100).IsRequired();
        builder.Property(mapping => mapping.Position).HasColumnName("position");
    }
}
