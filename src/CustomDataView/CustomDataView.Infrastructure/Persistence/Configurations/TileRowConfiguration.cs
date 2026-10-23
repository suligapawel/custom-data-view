using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class TileRowConfiguration : IEntityTypeConfiguration<TileRow>
{
    public void Configure(EntityTypeBuilder<TileRow> builder)
    {
        builder.ToTable("tiles");
        builder.HasKey(tile => tile.Id).HasName("pk_tiles");
        builder.Property(tile => tile.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(tile => tile.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(tile => tile.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
        builder.Property(tile => tile.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
        builder.HasIndex(tile => new { tile.CreatedAt, tile.Id }).HasDatabaseName("ix_tiles_created_at_id");
        builder.HasMany(tile => tile.Fields)
            .WithOne()
            .HasForeignKey(field => field.TileId)
            .HasConstraintName("fk_tile_fields_tile_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
