using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class TileBindingRowConfiguration : IEntityTypeConfiguration<TileBindingRow>
{
    public void Configure(EntityTypeBuilder<TileBindingRow> builder)
    {
        builder.ToTable("tile_bindings", table =>
        {
            table.HasCheckConstraint("ck_tile_bindings_target_kind", "target_kind IN ('parameter', 'input')");
            table.HasCheckConstraint("ck_tile_bindings_source_scope", "source_scope IN ('selected', 'all', 'item')");
            table.HasCheckConstraint("ck_tile_bindings_nodes", "source_node <> target_node");
            table.HasCheckConstraint("ck_tile_bindings_position", "position >= 0");
        });
        builder.HasKey(binding => new { binding.TileId, binding.TargetNode, binding.TargetKind, binding.TargetName })
            .HasName("pk_tile_bindings");
        builder.Property(binding => binding.TileId).HasColumnName("tile_id");
        builder.Property(binding => binding.TargetNode).HasColumnName("target_node").HasMaxLength(110);
        builder.Property(binding => binding.TargetKind).HasColumnName("target_kind").HasMaxLength(9);
        builder.Property(binding => binding.TargetName).HasColumnName("target_name").HasMaxLength(50);
        builder.Property(binding => binding.SourceNode).HasColumnName("source_node").HasMaxLength(50).IsRequired();
        builder.Property(binding => binding.SourceField).HasColumnName("source_field").HasMaxLength(100).IsRequired();
        builder.Property(binding => binding.SourceScope).HasColumnName("source_scope").HasMaxLength(8);
        builder.Property(binding => binding.Position).HasColumnName("position");
    }
}
