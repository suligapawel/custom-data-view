using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class TileActionRowConfiguration : IEntityTypeConfiguration<TileActionRow>
{
    public void Configure(EntityTypeBuilder<TileActionRow> builder)
    {
        builder.ToTable("tile_actions", table =>
        {
            table.HasCheckConstraint("ck_tile_actions_position", "position >= 0");
            table.HasCheckConstraint("ck_tile_actions_type", "type IN ('button', 'link')");
            table.HasCheckConstraint("ck_tile_actions_scope", "scope IN ('tile', 'item')");
        });
        builder.HasKey(action => new { action.TileId, action.Key }).HasName("pk_tile_actions");
        builder.Property(action => action.TileId).HasColumnName("tile_id");
        builder.Property(action => action.Key).HasColumnName("key").HasMaxLength(50);
        builder.Property(action => action.Position).HasColumnName("position");
        builder.Property(action => action.Label).HasColumnName("label").HasMaxLength(100).IsRequired();
        builder.Property(action => action.Type).HasColumnName("type").HasMaxLength(6).IsRequired();
        builder.Property(action => action.TargetTileId).HasColumnName("target_tile_id");
        builder.Property(action => action.Scope).HasColumnName("scope").HasMaxLength(4).IsRequired();
        builder.HasAlternateKey(action => new { action.TileId, action.Position }).HasName("uq_tile_actions_tile_id_position");
        builder.HasIndex(action => action.TargetTileId).HasDatabaseName("ix_tile_actions_target_tile_id");
        builder.HasOne<TileRow>()
            .WithMany()
            .HasForeignKey(action => action.TargetTileId)
            .HasConstraintName("fk_tile_actions_target_tile_id")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(action => action.Mappings)
            .WithOne()
            .HasForeignKey(mapping => new { mapping.TileId, mapping.ActionKey })
            .HasConstraintName("fk_tile_action_mappings_action")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
