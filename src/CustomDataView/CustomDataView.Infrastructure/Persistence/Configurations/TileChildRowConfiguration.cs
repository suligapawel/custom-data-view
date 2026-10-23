using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class TileChildRowConfiguration : IEntityTypeConfiguration<TileChildRow>
{
    public void Configure(EntityTypeBuilder<TileChildRow> builder)
    {
        builder.ToTable("tile_children", table =>
        {
            table.HasCheckConstraint("ck_tile_children_position", "position >= 0");
            table.HasCheckConstraint("ck_tile_children_scope", "scope IN ('tile', 'item')");
            table.HasCheckConstraint("ck_tile_children_load", "load IN ('eager', 'lazy')");
            table.HasCheckConstraint("ck_tile_children_not_self", "parent_tile_id <> child_tile_id");
        });
        builder.HasKey(child => new { child.ParentTileId, child.Key }).HasName("pk_tile_children");
        builder.Property(child => child.ParentTileId).HasColumnName("parent_tile_id");
        builder.Property(child => child.Key).HasColumnName("key").HasMaxLength(50);
        builder.Property(child => child.ChildTileId).HasColumnName("child_tile_id");
        builder.Property(child => child.Position).HasColumnName("position");
        builder.Property(child => child.CssClass).HasColumnName("css_class").HasMaxLength(200);
        builder.Property(child => child.Scope).HasColumnName("scope").HasMaxLength(4).IsRequired();
        builder.Property(child => child.Load).HasColumnName("load").HasMaxLength(5).IsRequired();
        builder.Property(child => child.Label).HasColumnName("label").HasMaxLength(100);
        builder.HasAlternateKey(child => new { child.ParentTileId, child.Position }).HasName("uq_tile_children_parent_tile_id_position");
        builder.HasIndex(child => child.ChildTileId).HasDatabaseName("ix_tile_children_child_tile_id");
        builder.HasOne<TileRow>()
            .WithMany()
            .HasForeignKey(child => child.ChildTileId)
            .HasConstraintName("fk_tile_children_child_tile_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
