using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class TileFieldRowConfiguration : IEntityTypeConfiguration<TileFieldRow>
{
    public void Configure(EntityTypeBuilder<TileFieldRow> builder)
    {
        builder.ToTable("tile_fields", table => table.HasCheckConstraint("ck_tile_fields_position", "position >= 0"));
        builder.HasKey(field => new { field.TileId, field.Position }).HasName("pk_tile_fields");
        builder.Property(field => field.TileId).HasColumnName("tile_id");
        builder.Property(field => field.Position).HasColumnName("position").ValueGeneratedNever();
        builder.Property(field => field.ConnectionId).HasColumnName("connection_id");
        builder.Property(field => field.Path).HasColumnName("path").HasColumnType("text").IsRequired();
        builder.Property(field => field.Label).HasColumnName("label").HasMaxLength(100).IsRequired();
        builder.HasIndex(field => field.ConnectionId).HasDatabaseName("ix_tile_fields_connection_id");
        builder.HasOne<ConnectionRow>()
            .WithMany()
            .HasForeignKey(field => field.ConnectionId)
            .HasConstraintName("fk_tile_fields_connection_id")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(field => field.Transformations)
            .WithOne()
            .HasForeignKey(transformation => new { transformation.TileId, transformation.FieldPosition })
            .HasConstraintName("fk_tile_field_transformations_field")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
